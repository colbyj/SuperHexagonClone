﻿using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Splits polygon made up of 2D points into individual triangles. From the Unity Wiki.
/// http://wiki.unity3d.com/index.php/Triangulator#C.23-_Triangulator.cs
/// </summary>
public class Triangulator
{
    public List<Vector2> m_points = new List<Vector2>();

    public Triangulator(Vector2[] points)
    {
        m_points = new List<Vector2>(points);
    }

    public Triangulator(Vector3[] points)
    {
        m_points = new List<Vector2>();

        for (int i = 0; i < points.Length; i++)
        {
            m_points.Add(new Vector2(points[i].x, points[i].y));
        }
    }

    /// <summary>
    /// Be sure to mainually set m_points
    /// </summary>
    public Triangulator()
    {
    }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        int n = m_points.Count;
        if (n < 3)
        {
            return indices.ToArray();
        }

        int[] V = new int[n];
        if (Area() > 0)
        {
            for (int v = 0; v < n; v++)
                V[v] = v;
        }
        else
        {
            for (int v = 0; v < n; v++)
                V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
            {
                return indices.ToArray();
            }

            int u = v;
            if (nv <= u)
            {
                u = 0;
            }

            v = u + 1;
            if (nv <= v)
            {
                v = 0;
            }

            int w = v + 1;
            if (nv <= w)
            {
                w = 0;
            }

            if (Snip(u, v, w, nv, V))
            {
                int a, b, c, s, t;
                a = V[u];
                b = V[v];
                c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                m++;
                for (s = v, t = v + 1; t < nv; s++, t++)
                    V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices.ToArray();
    }

    private float Area()
    {
        int n = m_points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = m_points[p];
            Vector2 qval = m_points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return (A * 0.5f);
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        int p;
        Vector2 A = m_points[V[u]];
        Vector2 B = m_points[V[v]];
        Vector2 C = m_points[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
        {
            return false;
        }

        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
            {
                continue;
            }

            Vector2 P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P))
            {
                return false;
            }
        }
        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 b, Vector2 c, Vector2 p)
    {
        float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
        float cCROSSap, bCROSScp, aCROSSbp;

        ax = c.x - b.x; ay = c.y - b.y;
        bx = A.x - c.x; by = A.y - c.y;
        cx = b.x - A.x; cy = b.y - A.y;
        apx = p.x - A.x; apy = p.y - A.y;
        bpx = p.x - b.x; bpy = p.y - b.y;
        cpx = p.x - c.x; cpy = p.y - c.y;

        aCROSSbp = ax * bpy - ay * bpx;
        cCROSSap = cx * apy - cy * apx;
        bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }

    public Mesh CreateMesh(bool invert = false)
    {
        Mesh mesh = new Mesh();

        int[] indices = Triangulate();

        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[m_points.Count];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(m_points[i].x, m_points[i].y, 0);
        }

        if (invert)
        {
            Array.Reverse(vertices);
        }

        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}