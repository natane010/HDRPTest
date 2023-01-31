using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BreakObject
{
	public class BallTrriger : MonoBehaviour
	{
		[SerializeField]
		private Breaker _breaker;

		private void Start()
		{
			if (_breaker == null)
			{
				_breaker = GetComponent<Breaker>();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			Debug.Log($"Trigger{other.name}");
			if (other.gameObject.tag == "Finish")
			{
				// collPoint = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position); // other.contacts[0].point;

				var fracture = other.gameObject.GetComponent<Fracture>();
				_breaker.Break(fracture, Vector3.zero);
			}
		}
        private void OnCollisionEnter(Collision collision)
        {
			Debug.Log($"Trigger{collision.collider.name}");
			if (collision.gameObject.tag == "Finish")
			{
				// collPoint = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position); // other.contacts[0].point;

				var fracture = collision.gameObject.GetComponent<Fracture>();
				_breaker.Break(fracture, Vector3.zero);
			}
		}
    }
}