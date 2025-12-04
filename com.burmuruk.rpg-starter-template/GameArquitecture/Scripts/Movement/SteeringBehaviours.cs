using Burmuruk.RPGStarterTemplate.Control;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement
{
    public class SteeringBehaviours
    {
        /// <summary>
        /// Returns the next position to move the agent towards the m_direction. The y ability es ignored.
        /// </summary>
        /// <param name="agent">Object that cointains the agent</param>
        /// <param name="targetPosition">Object that cointains the position</param>
        /// <returns></returns>
        public static Vector3 Seek2D(Movement agent, Vector3 targetPosition)
        {
            Vector3 desiredVel = targetPosition - agent.transform.position;
            return calculateSteer(agent, desiredVel);
        }

        /// <summary>
        /// Returns the next position to move the agent towards the m_direction.
        /// </summary>
        /// <param name="agent">Object that cointains the agent</param>
        /// <param name="targetPosition">Object that cointains the position</param>
        /// <returns></returns>
        public static Vector3 Seek3D(Movement agent, Vector3 targetPosition)
        {
            Vector3 desiredVel = targetPosition - agent.transform.position;
            return calculateSteer3D(agent, desiredVel);
        }

        /// <summary>
        /// Makes the given agent moven to the opposite side of the m_direction.  The y ability es ignored.
        /// </summary>
        /// <param name="agent">Object that cointains the agent</param>
        /// <param name="targetPosition">Object that cointains the position</param>
        /// <returns></returns>
        public static Vector3 Flee(Movement agent, Vector3 targetPosition)
        {
            Vector3 desiredVel = agent.transform.position - targetPosition;
            return calculateSteer(agent, desiredVel);
        }

        /// <summary>
        /// Starts to reduce the agent's velocity when reach the slowing radious and stop when it's the threshold.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="targetPosition"></param>
        /// <param name="slowingRadious"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static Vector3 Arrival(Movement agent, Vector3 targetPosition, float slowingRadious, float threshold)
        {
            Vector3 newVel = agent.GetComponent<Rigidbody>().velocity;
            float slowingCowficient;

            slowingCowficient = Vector3.Distance(agent.transform.position, targetPosition) is var dis && dis > threshold ? dis / slowingRadious : 0;

            return newVel.normalized * agent.getMaxVel() * slowingCowficient;
        }

        /// <summary>
        /// Calculate a random position to follow.
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        public static Vector3 Wander(Movement agent)
        {
            Vector3 velCopy = agent.transform.GetComponent<Rigidbody>().velocity;
            velCopy.Normalize();
            velCopy *= agent.wanderDisplacement;
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            randomDirection.Normalize();
            randomDirection *= agent.wanderRadious;
            randomDirection += velCopy;
            randomDirection += agent.transform.position;
            return randomDirection;
        }

        /// <summary>
        /// Moves the angent to the opposite direcion of the m_direction.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Vector3 Flee(Movement agent, Transform target)
        {
            Vector3 desiredVel = agent.transform.position - target.position;
            return calculateSteer(agent, desiredVel);
        }

        /// <summary>
        /// Calculates the steering of an object considering 2 dimensions
        /// </summary>
        public static Vector3 calculateSteer(Movement agent, Vector3 desiredVel)
        {
            Rigidbody agentRB = agent.GetComponent<Rigidbody>();
            desiredVel.Normalize();
            desiredVel *= agent.getMaxVel();
            Vector3 steering = desiredVel - agentRB.velocity;
            steering = truncate(steering, agent.getMaxSteerForce());
            steering /= agentRB.mass;
            steering += agentRB.velocity;
            steering = truncate(steering, agent.GetSpeed());
            steering.y = 0;
            return steering;
        }

        /// <summary>
        /// Calculates the steering of an object considering 3 dimensions
        /// </summary>
        public static Vector3 calculateSteer3D(Movement agent, Vector3 desiredVel)
        {
            Rigidbody agentRB = agent.GetComponent<Rigidbody>();
            desiredVel.Normalize();
            desiredVel *= agent.getMaxVel();
            Vector3 steering = desiredVel - agentRB.velocity;
            steering = truncate(steering, agent.getMaxSteerForce());
            steering /= agentRB.mass;
            steering += agentRB.velocity;
            steering = truncate(steering, agent.GetSpeed());
            return steering;
        }

        /// <summary>
        /// Rotates the agent to look at the given m_direction.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="rotationSpeed"></param>
        public static void LookAt(Transform agent, in Vector3 velocity, in float rotationSpeed)
        {
            //agent.transform.LookAt(agent.position + direction);
            Vector3 moveDir = velocity;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir.normalized);
                agent.rotation = Quaternion.Slerp(
                    agent.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
        }

        public static void LookAt(Transform agent, in Vector3 direction)
        {
            agent.transform.LookAt(agent.position + direction);
        }

        /// <summary>
        /// Ensures the giving vector's magnitud is not bigger than the maxValue.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        private static Vector3 truncate(Vector3 vector, float maxValue)
        {
            if (vector.magnitude <= maxValue)
            {
                return vector;
            }
            vector.Normalize();
            return vector *= maxValue;
        }

        public static Vector3 GetFollowPosition(Movement leader, Movement agent, float gap, params Character[] fellows)
        {
            if (!leader) return default;

            Vector3 rearPosition = GetRearPosition(leader, agent, gap);

            (float disBetween, Transform fellow) closest = (float.MaxValue, null);

            foreach (var fellow in fellows) 
            {
                if (Vector3.Distance(agent.transform.position, fellow.transform.position) is var d && d < closest.disBetween)
                {
                    closest = (d, fellow.transform);
                }
            }

            if (closest.disBetween < gap)
            {
                rearPosition += (agent.transform.position - closest.fellow.position).normalized * (gap);
            }

            return rearPosition;
        }

        private static Vector3 GetRearPosition(Movement leader, Movement agent, float gap)
        {
            Vector3 rearPosition = (leader.transform.position - agent.transform.position).normalized;
            rearPosition.Scale(Vector3.one * -1);
            rearPosition *= gap;
            rearPosition += leader.transform.position;

            return rearPosition;
        }
    }
}

