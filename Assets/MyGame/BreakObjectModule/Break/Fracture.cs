using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Burst;

namespace BreakObject
{


	[BurstCompile]
	public class Fracture : MonoBehaviour
	{
		public bool InitialMesh = true;
		public GameObject Prefab;
		public int PointCount = 1000000;
		public int ClusterCount = 20;

		[System.NonSerialized] private List<Vector3> points;

		private static ObjectPool<ConvexCalculator> calculators = new ObjectPool<ConvexCalculator>(() => new ConvexCalculator());
		private static ObjectPool<List<Vector3>> vertsPool = new ObjectPool<List<Vector3>>(() => new List<Vector3>());
		private static ObjectPool<List<int>> trisPool = new ObjectPool<List<int>>(() => new List<int>());
		private static ObjectPool<List<Vector3>> normalsPool = new ObjectPool<List<Vector3>>(() => new List<Vector3>());

		private bool _generatedMesh;
		private bool _disableAtFixedUpdate = false;
		private Task _meshGenerationTask;
		private InsideMeshCalculator _insideMeshCalc;

		private volatile float mass;
		private volatile Mesh mesh = null;
		private volatile List<Vector3> verts;
		private volatile List<int> tris;
		private volatile List<Vector3> normals;

		public float Mass { get { return mass; } }


		private void Start()
		{
			if (InitialMesh)
			{
				points = new List<Vector3>(PointCount);

				var mf = GetComponent<MeshFilter>();
				var mesh = mf.sharedMesh;
				var bounds = mesh.bounds;
				var calc = new InsideMeshCalculator(mesh);

				var verts = vertsPool.TakeOut();
				var tris = trisPool.TakeOut();

				mesh.GetVertices(verts);
				mesh.GetTriangles(tris, 0);
				mass = 1;

				points.AddRange(verts);

				for (int i = 0; i < points.Count - 1; i++)
				{
					var p0 = points[i];

					for (int j = i + 1; j < points.Count; j++)
					{
						var p1 = points[j];

						if ((p1 - p0).magnitude <= 0.00001f)
						{
							points.RemoveAt(j--);
						}
					}
				}

				while (points.Count < PointCount)
				{
					var point = new Vector3(
						Random.Range(bounds.min.x, bounds.max.x),
						Random.Range(bounds.min.y, bounds.max.y),
						Random.Range(bounds.min.z, bounds.max.z));

					if (calc.IsInside(point))
					{
						points.Add(point);
					}

					// if (point.sqrMagnitude <= 0.2025f)
					// {
					// 	points.Add(point);
					// }

					// var allInside = true;

					// for (int i = 0; i < tris.Count; i+=3)
					// {
					// 	var p0 = verts[tris[i]];
					// 	var p1 = verts[tris[i+1]];
					// 	var p2 = verts[tris[i+2]];

					// 	var normal = Vector3.Cross(p1 - p0, p2 - p0);
					// 	var testVector = point - p0;

					// 	var projection = Vector3.Dot(normal, testVector);

					// 	if (projection > 0.0f)
					// 	{
					// 		allInside = false;
					// 		break;
					// 	}
					// }

				}

				//StartGeneratingMesh();
			}
		}

		private void FixedUpdate()
		{
			if (_disableAtFixedUpdate)
			{
				gameObject.SetActive(false);
				Destroy(gameObject);
			}

			if (!InitialMesh && !_generatedMesh)
			{
				_meshGenerationTask.Wait();

				mesh.SetVertices(verts);
				mesh.SetNormals(normals);
				mesh.SetTriangles(tris, 0);

				vertsPool.PutBack(verts);
				trisPool.PutBack(tris);
				normalsPool.PutBack(normals);

				mesh.UploadMeshData(false);

				verts = null;
				normals = null;
				tris = null;

				var mf = GetComponent<MeshFilter>();
				var mc = GetComponent<MeshCollider>();
				var rb = GetComponent<Rigidbody>();

				mf.mesh = mesh;
				rb.mass = mass;

				if (mass < 0.03f)
				{

				}

				if (mc != null)
				{
					mc.sharedMesh = mesh;
				}

				_generatedMesh = true;
				mesh = null;
				_meshGenerationTask = null;
			}
		}

		private void StartGeneratingMesh()
		{
			if (points.Count < 4)
			{
				Debug.LogError("case: 1)");
			}
			else
			{
				mesh = new Mesh();
				mesh.name = "Shard";

				var scale = transform.lossyScale.x;

				_meshGenerationTask = Task.Run(() =>
				{
					var calc = calculators.TakeOut();
					var verts = vertsPool.TakeOut();
					var tris = trisPool.TakeOut();
					var normals = normalsPool.TakeOut();

					verts.Clear();
					tris.Clear();
					normals.Clear();

					calc.GenerateHull(points, ref verts, ref tris, ref normals);
					calculators.PutBack(calc);

					this.verts = verts;
					this.tris = tris;
					this.normals = normals;

					var vol = 0.0f;

				// set mass
				for (int i = 0; i < tris.Count; i += 3)
					{
						var p0 = scale * verts[tris[i]];
						var p1 = scale * verts[tris[i + 1]];
						var p2 = scale * verts[tris[i + 2]];

						var area = 0.5f * Vector3.Cross(p1 - p0, p2 - p0).magnitude;
						var normal = normals[i];

						vol += (1.0f / 3.0f) * Vector3.Dot(p0, normal) * area;
					}

					mass = vol;
				});
			}
		}

		public void DoFracture(Vector3 center)
		{
			if (points.Count >= ClusterCount)
			{
				var clusters = new List<Vector3>(ClusterCount);

				var maxRadius = 0.5f;

				while (clusters.Count < ClusterCount)
				{

					var dir = Random.onUnitSphere;
					var radius = Random.value * maxRadius;

					radius *= radius;

					var point = center + radius * dir;

					clusters.Add(point);
				}

				DoFracture(clusters);
			}
		}

		public void DoFracture()
		{
			var clusters = new List<Vector3>(ClusterCount);

			for (int i = 0; i < ClusterCount; i++)
			{
				clusters.Add(points[Random.Range(0, points.Count)]);
			}

			DoFracture(clusters);
		}

		private void DoFracture(List<Vector3> clusters)
		{
			_disableAtFixedUpdate = true;

			var velocity = GetComponent<Rigidbody>().velocity;

			var assignedClusters = new int[points.Count];

			var clouds = new List<Vector3>[ClusterCount];

			Parallel.For(0, points.Count, i =>
			{
				var closestCluster = -1;
				var closestClusterDistanceSqr = float.PositiveInfinity;

				for (int j = 0; j < ClusterCount; j++)
				{
					var distSqr = (points[i] - clusters[j]).sqrMagnitude;

					if (distSqr < closestClusterDistanceSqr)
					{
						closestCluster = j;
						closestClusterDistanceSqr = distSqr;
					}
				}

				assignedClusters[i] = closestCluster;
			});

			Parallel.For(0, ClusterCount, i =>
			{
				var cloud = new List<Vector3>();

				for (int j = 0; j < points.Count; j++)
				{
					if (assignedClusters[j] == i)
					{
						cloud.Add(points[j]);
					}
				}

				if (cloud.Count >= 4)
				{
					clouds[i] = cloud;
				}
			});

			var children = new Fracture[ClusterCount];

			for (int i = 0; i < ClusterCount; i++)
			{
				var child = Instantiate(Prefab, transform.parent);

				child.transform.localPosition = transform.localPosition;
				child.transform.localRotation = transform.localRotation;
				child.transform.localScale = transform.localScale;

				var frac = child.GetComponent<Fracture>();

				frac.Prefab = Prefab;
				//frac.ClusterCount = ClusterCount;
				frac.InitialMesh = false;

				children[i] = frac;
			}

			for (int i = 0; i < ClusterCount; i++)
			{
				if (clouds[i] == null)
				{
					children[i].gameObject.SetActive(false);
					Destroy(children[i].gameObject);
				}
				else
				{
					children[i].points = clouds[i];
					children[i].StartGeneratingMesh();
					children[i].GetComponent<Rigidbody>().velocity = velocity;
				}
			}
		}
	}
}