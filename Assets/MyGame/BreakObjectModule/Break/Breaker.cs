using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace BreakObject
{

	public class Breaker : MonoBehaviour
	{
		[SerializeField] protected float explosionRadius;
		[SerializeField] protected float explosionForce;


		public void Break(Fracture fracture, Vector3 world)
		{
			//var local = fracture.transform.InverseTransformPoint(world);
			Profiler.BeginSample("Do fracture call");

			//fracture.DoFracture(local);
			fracture.DoFracture();

			Profiler.EndSample();

			StartCoroutine(Explode(world));
		}

		private IEnumerator Explode(Vector3 worldPoint)
		{
			yield return null;
			yield return null;
			yield return null;

			foreach (var coll in Physics.OverlapSphere(worldPoint, explosionRadius))
			{
				var otherRb = coll.GetComponent<Rigidbody>();

				if (otherRb != null)
				{
					otherRb.AddExplosionForce(explosionForce, worldPoint, explosionRadius);
				}
			}
		}
	}
}
