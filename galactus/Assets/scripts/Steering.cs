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
    /// <returns>acceleration force vector</returns>
    /// <param name="position">current position</param>
    /// <param name="desiredLocation">desired location.</param>
    /// <param name="velocity">current velocity</param>
    /// <param name="desiredSpeed">desired speed, probably the max speed</param>
    /// <param name="maxAcceleration">the maximum allowed acceleration</param>
    /// <param name="deltaTime">time used to detect and compensate for over-acceleration</param>
    public static Vector3 Seek(Vector3 position, Vector3 desiredLocation, Vector3 velocity, float desiredSpeed, float maxAcceleration, float deltaTime)
    {
        Vector3 delta = desiredLocation - position;
        Vector3 desiredVelocity = delta.normalized * desiredSpeed;
//        Vector3 velocityDelta = desiredVelocity - velocity;
//        float velocityDeltaDist = velocityDelta.magnitude;
//        if (velocityDeltaDist < maxAcceleration * deltaTime)
//            return velocityDelta * (1 / deltaTime);
//        return velocityDelta * maxAcceleration * (1 / velocityDeltaDist);
		return SeekDirection(desiredVelocity, velocity, maxAcceleration, deltaTime);
    }

	public static Vector3 SeekDirectionNormal(Vector3 desiredVelocity, Vector3 velocity, float maxAcceleration, float deltaTime){
		Vector3 velocityDelta = desiredVelocity - velocity;
		float velocityDeltaDist = velocityDelta.magnitude;
		if (velocityDeltaDist < maxAcceleration * deltaTime)
			return Vector3.zero;//velocityDelta * (1 / (deltaTime*maxAcceleration));
		return velocityDelta.normalized;
	}

	public static Vector3 SeekDirection(Vector3 desiredVelocity, Vector3 velocity, float maxAcceleration, float deltaTime){
		Vector3 velocityDelta = desiredVelocity - velocity;
		float velocityDeltaDist = velocityDelta.magnitude;
		if (velocityDeltaDist < maxAcceleration * deltaTime)
			return velocityDelta * (1 / deltaTime);
		return velocityDelta * maxAcceleration * (1 / velocityDeltaDist);
	}

    public static Vector3 Flee(Vector3 position, Vector3 fleeLocation, Vector3 velocity, float desiredSpeed, float maxAcceleration, float deltaTime)
    {
        Vector3 delta = fleeLocation - position;
        Vector3 desiredVelocity = -delta.normalized * desiredSpeed;
        Vector3 velocityDelta = desiredVelocity - velocity;
        float velocityDeltaDist = velocityDelta.magnitude;
        if (velocityDeltaDist < maxAcceleration * deltaTime)
            return velocityDelta * (1 / deltaTime);
        return velocityDelta * maxAcceleration * (1 / velocityDeltaDist);
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
    /// <param name="targetLocation">Target location.</param>
    /// <param name="velocity">Velocity.</param>
    /// <param name="maxSpeed">Max velocity magnitude</param>
    /// <param name="maxSteering">Max steering is acceleration force: how quickly direction can be changed (including stop)</param>
    public static Vector3 Arrive(Vector3 position, Vector3 targetLocation, Vector3 velocity, float maxSpeed, float maxSteering, float deltaTime)
    {
        const float CLOSE_ENOUGH = 1.0f / 1024;
        Vector3 delta = targetLocation - position;
        float distanceFromTarget = delta.magnitude;
        float speed = velocity.magnitude;
        float moveThisTime = speed * deltaTime;
        float speedAdjustThisTime = maxSteering * deltaTime;
        if(distanceFromTarget < CLOSE_ENOUGH && moveThisTime < speedAdjustThisTime)
        {
            return velocity * (-1.0f / deltaTime);
        }
        Vector3 deltaSoon = targetLocation - (position + (velocity * deltaTime));
        float distanceFromTargetSoon = deltaSoon.magnitude;
        float brakeDistanceNeeded = (speed * speed) / (2 * maxSteering);
        float idealSpeed = maxSpeed;
        if (distanceFromTargetSoon <= brakeDistanceNeeded)
        {
            idealSpeed = speed - speedAdjustThisTime;
        }
        return Seek(position, targetLocation, velocity, idealSpeed, maxSteering, deltaTime);
    }
}
