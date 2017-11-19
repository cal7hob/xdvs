using UnityEngine;
using System.Collections;

public class ParticleBreaker : MonoBehaviour
{
	public float workTime;
	public float silenceTime;
	[Range(0f, 1f)]
	public float randomizeRatio = 1;

	void Awake()
	{
		if (!GetComponent<ParticleSystem>())
		{
			Debug.Log("No 'ParticleSystem' component for ParticleBreaker", gameObject);
			return;
		}

		StartCoroutine(Work());
	}


	private IEnumerator Work()
	{
		while (true)
		{
			yield return new WaitForSeconds(workTime * Random.Range(randomizeRatio, 1));
			GetComponent<ParticleSystem>().Stop();
			yield return new WaitForSeconds(silenceTime * Random.Range(randomizeRatio, 1));
			GetComponent<ParticleSystem>().Play();
		}
	}
}
