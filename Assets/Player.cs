using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public float m_fTimer;
    public eState m_nState;
    public float m_fDiveStartTime;
    public bool m_bSlowDown;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }
    private void Update()
    {
        switch (m_nState)
        {
            case eState.kMoveSlow:
                MoveSlow();
                break;
            case eState.kMoveFast:
                MoveFast();
                break;
            case eState.kDiving:
                Diving();
                break;
            case eState.kRecovering:
                Recovering();
                break;
        }
    }

    private void MoveSlow()
    {
        CheckForDive(); // Can Dive in this state
        UpdateDirectionAndSpeed(); // Update the direciton and speed
        m_fSpeed = m_fTargetSpeed; // Make sure speed is what the player is requesting

        // If the slowdown timer is going, you can't transition to fast
        if (m_fTimer <= 0)
        {
            if (m_fTargetSpeed == m_fMaxSpeed)
            {
                // Start the move fast operation
                m_nState = eState.kMoveFast;
                return;
            }
        }
        else
        {
            m_fTimer -= Time.deltaTime;
            m_fSpeed = m_fSlowSpeed; // Ensure slower speed
        }

        transform.rotation = Quaternion.Euler(0, 0, m_fTargetAngle); // Rotate to the target angle
        transform.position += transform.right * m_fSpeed * -75 * Time.deltaTime; // Move to the target position scaled with speed
    }

    private void MoveFast()
    {
        CheckForDive(); // Can Dive in this state
        UpdateDirectionAndSpeed(); // Update the direciton and speed

        // If the slowdown sequence should be running, decrement the speed scaled by Time.deltaTime
        if (m_bSlowDown)
        {
            m_fSpeed -= Time.deltaTime * 0.1f;
            if (m_fSpeed <= m_fSlowSpeed)
            {
                m_bSlowDown = false; // Once the speed is slow enough, stop slowing down
            }
        }
        else
        {
            // Check for the transition to slow state when speed is slow enough
            if (m_fSpeed <= m_fSlowSpeed)
            {
                m_fTimer = 0.5f; // This timer is important for the slow state to not instantly transition to fast again
                m_fSpeed = m_fSlowSpeed;
                m_nState = eState.kMoveSlow;
                return;
            }
            // Detect a slowdown when the delta angle is greater than the threshold tuneable
            if (Mathf.Abs(Mathf.DeltaAngle(transform.rotation.eulerAngles.z, m_fTargetAngle)) > m_fFastRotateMax)
            {
                m_bSlowDown = true;
            }
            transform.rotation = Quaternion.Euler(0, 0, m_fTargetAngle); // Rotate the player
        }
        transform.position += transform.right * m_fSpeed * -75 * Time.deltaTime; // Move the player
    }

    private void Diving()
    {
        // If time is up, start recovering
        if (Time.time - m_fDiveStartTime >= m_fDiveTime)
        {
            m_nState = eState.kRecovering;
            return;
        }

        // Calculate the fraction of the dive time that has passed
        float t = (Time.time - m_fDiveStartTime) / m_fDiveTime;

        // Lerp towards the end position
        transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, t);
    }

    private void Recovering()
    {
        // If the time since dive start is greater than dive time and recovery time total, time to transition
        if (Time.time - m_fDiveStartTime >= m_fDiveTime + m_fDiveRecoveryTime)
        {
            m_nState = eState.kMoveSlow;
            return;
        }
    }
    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }
}
