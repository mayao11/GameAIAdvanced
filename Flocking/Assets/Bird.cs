using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FlyType
{
    Seek,             // 飞向指定目标
    CustomSeek,       // 改进的Seek，已废弃
    Arrive,           // 快到达目标时减速
    Flee,             // 逃跑
    Wander,           // 无目的漫游
    Persuit,          // 追逐
    Flocking,         // 基本集群行动
    Flocking2,        // 加入目标的集群行动
}

public class Bird : MonoBehaviour {

    public Rigidbody rigid;
    Animation anim;

    public FlyType flyType;

    float maxSpeed = 3.0f;
    float maxTurnSpeed = 5.0f;
    
	void Start () {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animation>();
	}

    Vector3 Seek(Vector3 targetPosition)
    {
        Vector3 diff = targetPosition - transform.position;
        Vector3 desiredVelocity = diff.normalized * maxSpeed;

        return desiredVelocity - rigid.velocity;
    }
	
    Vector3 CustomSeek(Vector3 targetPosition)
    {
        Vector3 diff = targetPosition - transform.position;
        Vector3 force;
        Vector3 fwd = rigid.velocity.normalized;
        Vector3 diff1 = Vector3.Dot(diff, fwd.normalized) * fwd.normalized;
        Vector3 diff2 = diff - diff1;

        if (diff1.magnitude > maxSpeed)
        {
            diff1 = diff1.normalized * maxSpeed;
        }
        if (diff2.magnitude > maxTurnSpeed)
        {
            diff2 = diff2.normalized * maxTurnSpeed;
        }

        if (Vector3.Dot(diff, fwd) < 0)
        {
            // 后面
            force = diff1 * 0.5f + diff2;
        }
        else
        {
            // 前面
            force = diff1 + diff2;
        }

        if (rigid.velocity.magnitude > maxSpeed)
        {
            rigid.velocity = rigid.velocity.normalized * maxSpeed;
        }
        //rigid.AddForce(force);

        //transform.LookAt(rigid.velocity.normalized + force.normalized);
        return force;
    }

    Vector3 Arrive(Vector3 targetPosition)
    {
        Vector3 diff = targetPosition - transform.position;
        float dist = diff.magnitude;       

        if (dist <= 0)
        {
            return Vector3.zero;
        }

        float speed = dist / 1.5f;
        if (speed>maxSpeed) speed=maxSpeed;

        return diff.normalized * speed - rigid.velocity;
    }
    Vector3 Flee(Vector3 targetPosition)
    {
        Vector3 diff = targetPosition - transform.position;
        if (diff.magnitude > 3)
        {
            return Vector3.zero;
        }
        Vector3 desiredVelocity = -diff.normalized * maxSpeed;

        return desiredVelocity - rigid.velocity;
    }


    Vector3 Wander()
    {
        Vector3 v = Random.onUnitSphere;
        
        Vector3 wanderTarget = transform.forward + v;

        wanderTarget += transform.position;

        //Debug.DrawLine(transform.position, wanderTarget, Color.green, 0);

        //Vector3 ret = wanderTarget - transform.position;
        Vector3 wanderForce = Seek(wanderTarget);
       
        return wanderForce;
    }

    Vector3 Persuit(Transform evader)
    {
        Vector3 diff = evader.position - transform.position;
        Rigidbody evaderRigid = evader.GetComponent<Rigidbody>();
        float predictTime = diff.magnitude / (rigid.velocity - evaderRigid.velocity).magnitude;
        Vector3 predictPos = predictTime * evaderRigid.velocity + evader.position;

        return Seek(predictPos);
    }

    void CheckResetPosition()
    {
        if (transform.position.magnitude > 7)
        {
            transform.position = Vector3.zero;
            rigid.velocity = Vector3.zero;
        }
    }

    Vector3 Separation(List<Transform> birds)
    {
        if (birds.Count == 0)
        {
            return Vector3.zero;
        }
        Vector3 ret = Vector3.zero;
        foreach (var bird in birds)
        {
            Vector3 to = transform.position - bird.position;
            // 意思是：本鸟要远离所有人，离我越近的越要远离
            ret += to.normalized / to.magnitude;
        }
        return ret;
    }

    Vector3 Alignment(List<Transform> birds)
    {
        if (birds.Count == 0)
        {
            return Vector3.zero;
        }
        Vector3 average = Vector3.zero;
        int num = 0;
        foreach (var bird in birds)
        {
            average += bird.GetComponent<Rigidbody>().velocity.normalized;
            num++;
        }
        return average / num;
    }

    Vector3 Cohesion(List<Transform> birds)
    {
        if (birds.Count == 0)
        {
            return Vector3.zero;
        }
        Vector3 center = Vector3.zero;
        int num = 0;
        foreach (var bird in birds)
        {
            center += bird.position;
            num++;
        }
        center /= num;
        Vector3 force = Seek(center);
        return force;
    }

    float flockingDist = 5.0f;
    Vector3 Flocking()
    {
        List<Transform> neighbours = new List<Transform>();
        GameObject[] birds = GameObject.FindGameObjectsWithTag("Bird");
        foreach (var bird in birds)
        {
            if (bird == gameObject)
            {
                continue;
            }
            if (Vector3.Distance(transform.position, bird.transform.position) < flockingDist)
            {
                neighbours.Add(bird.transform);
            }
        }
        Vector3 force = Vector3.zero;
        force += Separation(neighbours);
        force += Alignment(neighbours);
        force += Cohesion(neighbours)*2;
        return force;
    }


    void Update()
    {
        //CheckResetPosition();

        Vector3 steeringForce = new Vector3();
        switch (flyType)
        {
            case FlyType.Seek:
                steeringForce = Seek(Manager.targetPosition);
                break;
            case FlyType.CustomSeek:
                steeringForce = CustomSeek(Manager.targetPosition);
                break;
            case FlyType.Arrive:
                steeringForce = Arrive(Manager.targetPosition);
                break;
            case FlyType.Flee:
                steeringForce = Flee(Manager.targetPosition);
                break;
            case FlyType.Wander:
                steeringForce = Wander();
                break;
            case FlyType.Persuit:
                GameObject evader = GameObject.Find("FreeBird");
                if (evader != null)
                {
                    steeringForce = Persuit(evader.transform);
                }
                break;
            case FlyType.Flocking:
                steeringForce = Flocking();
                break;
            case FlyType.Flocking2:
                steeringForce = Flocking();
                steeringForce += Seek(Manager.targetPosition);
                break;
        }

        anim["fly"].speed = Mathf.Lerp(0.3f, 1.5f, steeringForce.magnitude / 8.0f);


        // 限制掉头的力，鸟不能直接倒车 =.=

        Vector3 velocityNormalized = rigid.velocity.normalized;
            // 沿速度方向的大小
        Vector3 f1 = Vector3.Dot(steeringForce, velocityNormalized) * velocityNormalized; 
        if (Vector3.Dot(steeringForce, velocityNormalized) < 0)
        {
            // 直接减掉向后的力
            steeringForce -= f1;
        }

        rigid.AddForce(steeringForce);

        // 设定角度，带有平滑
        Vector3 mid = Vector3.Lerp(transform.forward, rigid.velocity.normalized, 0.1f);
        transform.LookAt(transform.position + mid);

        // 速度限制
        if (rigid.velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            rigid.velocity = rigid.velocity.normalized * maxSpeed;
        }
    }
}
