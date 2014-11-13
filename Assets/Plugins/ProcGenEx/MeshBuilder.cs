using MathEx;
using System;
using System.Collections.Generic;
using SystemEx;
using UnityEngine;
using UnityEngineEx;

namespace ProcGenEx
{
	public class TriangleGraph
	{
		public struct Node
		{
			public int i;
			public List<int> links;
		}

		Dictionary<int, Node> nodes = new Dictionary<int, Node>();

		public TriangleGraph(List<int> triangles)
		{
			var sides = new Dictionary<Tuple<int, int>, List<int>>();
			Action<Tuple<int, int>, int> sidesIn = (Tuple<int, int> p, int ti) => {
				List<int> tp;
				if (!sides.TryGetValue(p.Sort(), out tp)) {
					tp = new List<int>(2);
					tp.Add(ti);
					sides.Add(p.Sort(), tp);
				}
				else {
					tp.Add(ti);
				}
			};
			Action<int, int> nodesIn = (int t0, int t1) => {
				Node n;
				if (!nodes.TryGetValue(t0, out n)) {
					n = new Node();
					n.i = t0;
					n.links = new List<int>(3);
					n.links.Add(t1);
					nodes.Add(t0, n);
				}
				else {
					n.links.Add(t1);
					nodes[t0] = n;
				}
			};
			
			for (int i = 0; i < triangles.Count; i++) {
				var ta = triangles[i];
				var tb = triangles[i + 1];
				var tc = triangles[i + 2];

				sidesIn(Tuple.Create(ta, tb), i);
				sidesIn(Tuple.Create(tb, tc), i);
				sidesIn(Tuple.Create(tc, ta), i);
			}

			foreach (var link in sides.Values) {
				nodesIn(link[0], link[1]);
				nodesIn(link[1], link[0]);
			}
		}
	}

	public class MeshBuilder
	{
		public List<vec3> vertices = null;
		public List<vec3> normals = null;
		public List<vec2> uvs = null;
		public List<int> triangles = null;

		public MeshBuilder(int VertexCount, int TriangleCount)
		{
			vertices = new List<vec3>(VertexCount);
			normals = new List<vec3>(VertexCount);
			uvs = new List<vec2>(VertexCount);
			triangles = new List<int>(TriangleCount * 3);
		}

		public Mesh ToMesh()
		{
			Mesh m = new Mesh();

			m.vertices = vertices.ConvertAll<Vector3>(v => v.ToVector3()).ToArray();
			m.normals = normals.ConvertAll<Vector3>(v => v.ToVector3()).ToArray();
			m.uv = uvs.ConvertAll<Vector2>(v => v.ToVector2()).ToArray();
			m.triangles = triangles.ToArray();

			m.Apply();
			
			return m;
		}

		public void Grow(int VertexCount, int TriangleCount)
		{
			int dvc = Math.Max(vertices.Capacity, vertices.Count + VertexCount);
			int dtc = Math.Max(triangles.Capacity, triangles.Count + TriangleCount * 3);
			vertices.Capacity = dvc;
			normals.Capacity = dvc;
			uvs.Capacity = dvc;
			triangles.Capacity = dtc;
		}

#region Simple figures

		public int[] AddTriangle(vec3 a, vec3 b, vec3 c)
		{
			int[] result = null;
			return AddTriangle(a, b, c, ref result);
		}

		public int[] AddTriangle(vec3 a, vec3 b, vec3 c, ref int[] result)
		{		
			if (result == null)
				result = new int[3];

			var n = vec3.Cross((c - a), (a - b)).normalized;

			Grow(3, 1);
			result[0] = CreateVertex(a, n);
			result[1] = CreateVertex(b, n);
			result[2] = CreateVertex(c, n);

			MakeTriangle(result[0], result[1], result[2]);

			return result;
		}

		public int[] AddTriangle(vec3[] v, vec2[] uv)
		{
			int[] result = null;
			return AddTriangle(v, uv, ref result);
		}

		public int[] AddTriangle(vec3[] v, vec2[] uv, ref int[] result)
		{
			if (result == null)
				result = new int[3];
			
			var n = vec3.Cross((v[2] - v[0]), (v[0] - v[1])).normalized;

			Grow(3, 1);
			result[0] = CreateVertex(v[0], n, uv[0]);
			result[1] = CreateVertex(v[1], n, uv[1]);
			result[2] = CreateVertex(v[2], n, uv[2]);

			MakeTriangle(result[0], result[1], result[2]);
			
			return result;
		}

		public int[] AddQuad(vec3 a, vec3 b, vec3 c, vec3 d)
		{
			int[] result = null;
			return AddQuad(a, b, c, d, ref result);
		}

		public int[] AddQuad(vec3 a, vec3 b, vec3 c, vec3 d, ref int[] result)
		{
			if (result == null)
				result = new int[4];
			
			var n = vec3.Cross((c - a), (a - b)).normalized;

			Grow(4, 2);
			result[0] = CreateVertex(a, n);
			result[1] = CreateVertex(b, n);
			result[2] = CreateVertex(c, n);
			result[3] = CreateVertex(d, n);

			MakeQuad(result[0], result[1], result[2], result[3]);
			
			return result;
		}

		public int[] AddQuad(vec3[] v, vec2[] uv)
		{
			int[] result = null;
			return AddQuad(v, uv, ref result);
		}

		public int[] AddQuad(vec3[] v, vec2[] uv, ref int[] result)
		{
			if (result == null)
				result = new int[4];
			
			var n = vec3.Cross((v[2] - v[0]), (v[0] - v[1])).normalized;

			Grow(4, 2);
			result[0] = CreateVertex(v[0], n, uv[0]);
			result[1] = CreateVertex(v[1], n, uv[1]);
			result[2] = CreateVertex(v[2], n, uv[2]);
			result[3] = CreateVertex(v[3], n, uv[3]);

			MakeQuad(result[0], result[1], result[2], result[3]);
			
			return result;
		}

		public int[] AddPlane(Plane p, vec3 origin, vec2 size, vec2i step)
		{
			return AddPlane(p, origin, size, step, vec3.forward);
		}

		public int[] AddPlane(Plane p, vec3 origin, vec2 size, vec2i step, vec3 forward)
		{
			var n = MathEx.Convert.ToVec3(p.normal);
			vec3 right = vec3.Cross(n, forward);
			int cn = step.x + 1;
			int rn = step.y + 1;
			int vn = cn * rn;
			int tn = step.x * step.y * 2;
			vec2 dv = size.Div(step);

			int[] result = new int[vn];

			Grow(vn, tn);

			int ri = 0;
			vec3 v;
			for (int i = 0; i < cn; i++) {
				v = origin + i * right * dv.x;
				for (int j = 0; j < rn; j++, v += forward * dv.y) {					
					result[ri++] = CreateVertex(v, n);
				}
			}

			int vi = 0;
			for (int i = 0; i < tn / 2; i++, vi++) {
				if (((vi + 1) % rn) == 0)
					vi++;

				MakeQuad(result[vi + 0], result[vi + 1], result[vi + 1 + rn], result[vi + 0 + rn]);
			}

			return result;
		}

#endregion

		public int CreateVertex(vec3 v, vec3 n)
		{
			return CreateVertex(v, n, vec2.empty);
		}

		public int CreateVertex(vec3 v, vec3 n, vec2 u)
		{
			var vi = vertices.Count;

			vertices.Add(v);
			normals.Add(n);
			uvs.Add(u);

			return vi;
		}

		public int CopyVertex(int vi)
		{
			return CreateVertex(vertices[vi], normals[vi], uvs[vi]);
		}

		public int CopyVertex(int vi, vec3 dv)
		{
			return CreateVertex(vertices[vi] + dv, normals[vi], uvs[vi]);
		}

		public void MakeTriangle(int a, int b, int c)
		{
			var ti = triangles.Count;			

			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
		}

		public void MakeQuad(int a, int b, int c, int d)
		{
			MakeTriangle(a, b, c);
			MakeTriangle(a, c, d);
		}

		public void MakeFan(params int[] ps)
		{
			for (int i = 1; i < ps.Length - 1; i++) {
				MakeTriangle(ps[0], ps[i], ps[i + 1]);
			}
		}

		public void Extrude(int[] contour, vec3 direction, int steps)
		{
			// light overgrow in triangle count expected.
			Grow(contour.Length * steps, contour.Length * steps * 2);

			vec3 dv = direction / steps;
			for (int si = 0; si < steps; si++) {
				int pv = CopyVertex(contour[0], dv);
				for (int i = 1; i < contour.Length; i++) {
					int cv = CopyVertex(contour[i], dv);

					MakeQuad(contour[i-1], pv, cv, contour[i]);

					contour[i - 1] = pv;
					pv = cv;
				}
				contour[contour.Length - 1] = pv;
			}
		}

		public void Slice(Plane plane)
		{
			int[] v2v = new int[vertices.Count].Initialize(-1);
			List<vec3> vs = vertices;
			List<vec3> ns = normals;
			List<vec2> us = uvs;
			List<int> ts = triangles;

			vertices = new List<vec3>(vs.Count);;
			normals = new List<vec3>(vs.Count);
			uvs = new List<vec2>(vs.Count);
			triangles = new List<int>(ts.Count);


			for (int i = 0; i < vs.Count; i++) {
				if (plane.GetDistanceToPoint(MathEx.Convert.ToVector3(vs[i])) >= 0) {
					v2v[i] = CreateVertex(vs[i], ns[i], us[i]);					
				}
			}


			for (int i = 0; i < ts.Count; i += 3) {
				int st = ((v2v[ts[i]] < 0 ? 0 : 1) << 0) + ((v2v[ts[i + 1]] < 0 ? 0 : 1) << 1) + ((v2v[ts[i + 2]] < 0 ? 0 : 1) << 2);
				
				if (st == 0)
					continue;
				if (st == 7) {
					MakeTriangle(v2v[ts[i]], v2v[ts[i + 1]], v2v[ts[i + 2]]);
					continue;
				}

				vec3 ab, bc, ca;
				float abd, bcd, cad;
				bool abi = plane.Intersect(vs[ts[i]], vs[ts[i + 1]], out ab, out abd);
				bool bci = plane.Intersect(vs[ts[i + 1]], vs[ts[i + 2]], out bc, out bcd);
				bool cai = plane.Intersect(vs[ts[i + 2]], vs[ts[i]], out ca, out cad);


				List<int> nvs = new List<int>(4);

				if (!(v2v[ts[i]] < 0)) {
					nvs.Add(v2v[ts[i]]);
				}
				if (abi && (abd > 0 && abd < 1)) {
					nvs.Add(CreateVertex(ab, MathEx.MathEx.Slerp(ns[ts[i]], ns[ts[i + 1]], abd)));
				}

				if (!(v2v[ts[i + 1]] < 0)) {
					nvs.Add(v2v[ts[i + 1]]);
				}
				if (bci && (bcd > 0 && bcd < 1)) {
					nvs.Add(CreateVertex(bc, MathEx.MathEx.Slerp(ns[ts[i + 1]], ns[ts[i + 2]], bcd)));
				}

				if (!(v2v[ts[i + 2]] < 0)) {
					nvs.Add(v2v[ts[i + 2]]);
				}
				if (cai && (cad > 0 && cad < 1)) {
					nvs.Add(CreateVertex(ca, MathEx.MathEx.Slerp(ns[ts[i + 2]], ns[ts[i]], cad)));
				}

				MakeFan(nvs.ToArray());
			}
		}

		public int[] Select(Plane plane)
		{
			List<int> result = new List<int>(vertices.Count);

			for (int i = 0; i < vertices.Count; i++) {
				if (plane.GetDistanceToPoint(MathEx.Convert.ToVector3(vertices[i])) >= 0) {
					result.Add(i);
				}
			}

			return result.ToArray();
		}

		public List<int> Select(Plane plane, out List<int> sidea, out List<int> sideb)
		{
			sidea = new List<int>(vertices.Count);
			sideb = new List<int>(vertices.Count);

			for (int i = 0; i < vertices.Count; i++) {
				if (plane.GetDistanceToPoint(MathEx.Convert.ToVector3(vertices[i])) >= 0) {
					sidea.Add(i);
				}
				else 
					sideb.Add(i);
			}

			return sidea;
		}

		public List<int> Select(ray r, float radius, out List<int> sidea)
		{
			sidea = new List<int>(vertices.Count);
			
			for (int i = 0; i < vertices.Count; i++) {
				if (r.distance(vertices[i]) < radius) {
					sidea.Add(i);
				}
			}

			return sidea;
		}

		public List<int> Select(ray r, float radius, out List<int> sidea, out List<int> sideb)
		{
			sidea = new List<int>(vertices.Count);
			sideb = new List<int>(vertices.Count);

			for (int i = 0; i < vertices.Count; i++) {
				if (r.distance(vertices[i]) < radius) {
					sidea.Add(i);
				}
				else 
					sideb.Add(i);
			}

			return sidea;
		}

		public int[] Project(Plane plane)
		{
			List<int> contour = new List<int>();



			return contour.ToArray();
		}

		public void UVMapPlane(Plane plane, vec3 forward, int[] vs)
		{
			UVMapPlane(plane, forward, aabb2.one, vs);
		}

		public void UVMapPlane(Plane plane, vec3 forward, aabb2 uvrect, int[] vs)
		{
			vec2[] pvs = new vec2[vs.Length];

			aabb2 b = aabb2.empty;
			Quaternion q = Quaternion.LookRotation(MathEx.Convert.ToVector3(forward), plane.normal);
			for (int i = 0; i < vs.Length; i++) {
				pvs[i] = MathEx.Convert.ToVec3(q * MathEx.Convert.ToVector3(vertices[vs[i]])).xz();
				b = b.Extend(pvs[i]);
			}

			Debug.Log(b);
			for (int i = 0; i < vs.Length; i++) {
				uvs[vs[i]] = uvrect.a + uvrect.size.Mul((pvs[i] - b.a).Div(b.size));
				Debug.Log(uvs[vs[i]]);
			}
		}
	}
}
