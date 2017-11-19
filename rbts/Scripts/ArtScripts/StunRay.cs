using System.Collections;
using System.Collections.Generic;
using Pool;
using UnityEngine;

public class StunRay : PoolObject
{
    public float speed = 1.0f;
	public float force = 10.0f;

    private Transform target;
    private Transform emit;
    private ParticleSystem.Particle[] particles;
    private int particleCount;
    private bool activated;

    private ParticleSystem ps;


	void Awake ()
    {
        ps = GetComponent<ParticleSystem> ();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }
    
	void Update ()
	{
	    if (!activated)
	        return;

        if (target == null || emit == null)
	    {
	        ReturnObject();
	        return;
	    }

        particleCount = ps.GetParticles (particles);

	    float emitDistance = (target.position - emit.position).magnitude;

        for (int i = 0; i < particleCount; i++)
        {
            ParticleSystem.Particle p = particles [i];
/*            if (Vector3.SqrMagnitude(p.position - emit.position) / sqrEmitDistance > 0.81f) // частица прошла больше 90% пути до цели
            {
                Stop();
                return;
            }*/

            Vector3 directionToTarget = (target.position - p.position).normalized;
            Vector3 seekForce = directionToTarget * force * Time.deltaTime;
            p.velocity = (p.velocity + seekForce).normalized * speed * emitDistance;
            particles [i] = p;
		}

		ps.SetParticles (particles, particleCount);
	}

    public void Activate(Transform owner, Transform target)
    {
        activated = true;
        emit = owner;
        this.target = target;

        gameObject.SetActive(true);
        ps.Play(true);
    }


    public void Stop()
    {
        ReturnObject();
    }

    public override void ReturnObject()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        emit = null;
        target = null;
        activated = false;

        base.ReturnObject();
    }
}
