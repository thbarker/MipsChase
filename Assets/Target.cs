using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }
    void Update()
    {
        switch (m_nState)
        {
            case eState.kIdle:
                CheckProximity();
                break;
            case eState.kHopStart:
                StartHop();
                break;
            case eState.kHop:
                PerformHop();
                break;
            case eState.kCaught:
                // Everything needed for being caught is handled in OnTriggerStay2D
                break;
        }
    }

    private void CheckProximity()
    {
        // Switch states when within range
        if (Vector3.Distance(transform.position, m_player.transform.position) < m_fScaredDistance)
        {
            m_nState = eState.kHopStart;
        }
    }

    private void StartHop()
    {
        // Store important data before actually hopping
        m_fHopStart = Time.time;
        m_vHopStartPos = transform.position;
        Vector3 direction = (transform.position - m_player.transform.position).normalized;
        float distance = m_fScaredDistance * 0.5f;

        // Generate possible hop directions and select a valid one
        Vector3 chosenDirection = GenerateValidDirection(m_vHopStartPos, direction, distance);
        m_vHopEndPos = m_vHopStartPos + chosenDirection * distance;

        // Update rotation to face the hop direction on the Z axis
        float angle = Mathf.Atan2(chosenDirection.y, chosenDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // Switch to Hop State
        m_nState = eState.kHop;
    }

    private Vector3 GenerateValidDirection(Vector3 start, Vector3 initialDirection, float distance)
    {
        List<Vector3> validDirections = new List<Vector3>();
        const int numberOfDirections = 36; // Checking 36 different angles (10 degree increments)
        float angleIncrement = 360.0f / numberOfDirections;

        // Generate directions in a semi-circle away from the player
        for (float angle = 0; angle < 180.0f; angle += angleIncrement)
        {
            Vector3 rotatedDirection1 = Quaternion.Euler(0, 0, angle) * initialDirection;
            Vector3 rotatedDirection2 = Quaternion.Euler(0, 0, -angle) * initialDirection;

            if (IsDirectionValid(start, rotatedDirection1, distance))
            {
                validDirections.Add(rotatedDirection1);
            }
            if (IsDirectionValid(start, rotatedDirection2, distance))
            {
                validDirections.Add(rotatedDirection2);
            }
        }

        // Randomly choose from valid directions
        if (validDirections.Count > 0)
        {
            int randomIndex = Random.Range(0, validDirections.Count);
            return validDirections[randomIndex];
        }

        // Fall back to the initial direction if no valid directions are found
        return initialDirection;
    }

    private bool IsDirectionValid(Vector3 start, Vector3 direction, float distance)
    {
        // Check if the direction and distance would take the rabbit offscreen
        Vector3 end = start + direction * distance;
        Vector3 screenEnd = Camera.main.WorldToViewportPoint(end);
        return screenEnd.x > 0 && screenEnd.x < 1 && screenEnd.y > 0 && screenEnd.y < 1;
    }

    private void PerformHop()
    {
        // Lerp the position until finished
        float timeSinceStarted = Time.time - m_fHopStart;
        float percentageComplete = timeSinceStarted / m_fHopTime;

        if (percentageComplete >= 1.0f)
        {
            // Finalize final position and switch state
            transform.position = m_vHopEndPos;
            m_nState = eState.kIdle;
        }
        else
        {
            transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, percentageComplete);
        }
    }
    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }
}