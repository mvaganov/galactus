using UnityEngine;

public class Steering
{
    public static Vector2 RandomUnitVector2()
    {
        Vector2 randomUnitVector;
        do {
            randomUnitVector = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        } while (randomUnitVector == Vector2.zero);
        return randomUnitVector.normalized;
    }


    public static Vector3 RandomUnitVector3()
    {
        return Quaternion.Euler(Random.Range(-180f, 180f), Random.Range(-180f, 180f), 0) * Vector3.up;
    }

    public static float CLOSE_ENOUGH = 1.0f / 256.0f;
    /// <summary>the direction that should be accelerated toward</summary>
    /// <returns>The steering direction as a unit vector</returns>
    /// <param name="position">current position</param>
    /// <param name="velocity">current velocity</param>
    /// <param name="desiredSpeed">Desired speed, probably the max speed</param>
    /// <param name="desiredLocation">Desired location.</param>
    public static Vector3 Seek(Vector3 position, Vector3 velocity, float desiredSpeed, Vector3 desiredLocation)
    {
        Vector3 delta = desiredLocation - position;
        //float distance = delta.magnitude;
        Vector3 desiredVelocity = delta.normalized * desiredSpeed;
        Vector3 velocityDelta = desiredVelocity - velocity;
        if (velocityDelta.sqrMagnitude < CLOSE_ENOUGH)
        {
            return Vector3.zero;
        }
        return velocityDelta.normalized;
    }

    //	public static Vector3 Seek(Vector3 target, Agent agent) {
    //		Vector3 desired = target - agent.position;
    //		desired *= agent.maxSpeed / desired.magnitude;
    //		Vector3 force = desired - agent.velocity;
    //		return force * (agent.maxForce / agent.maxSpeed);
    //	}

    public static float BrakeDistance(float speed, float acceleration)
    {
        return (speed * speed) / (2 * acceleration);
    }
    /// <summary>Arrive the specified position, velocity, maxVelocity, maxSteering and targetLocation.</summary>
    /// <param name="position">Position.</param>
    /// <param name="velocity">Velocity.</param>
    /// <param name="maxVelocity">Max velocity.</param>
    /// <param name="maxSteering">Max steering, how quickly direction can be changed (including stop)</param>
    /// <param name="targetLocation">Target location.</param>
    public static Vector3 Arrive(Vector3 position, Vector3 velocity, float maxVelocity, float maxSteering, Vector3 targetLocation)
    {
        Vector3 delta = targetLocation - position;
        float distanceFromTarget = delta.magnitude;
        float speed = velocity.magnitude;
        float brakeDistanceNeeded = (speed * speed) / (2 * maxSteering);
        if (distanceFromTarget < CLOSE_ENOUGH)
        {
            return Vector3.zero;
        }
        if (distanceFromTarget <= brakeDistanceNeeded)
        {
            return -velocity; // stop!
        }
        return Seek(position, velocity, maxVelocity, targetLocation);
    }
}
