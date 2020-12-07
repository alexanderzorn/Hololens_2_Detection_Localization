using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public class RaysClosest : MonoBehaviour
{

    public class Ray
    {
        public Ray(Vector3 inp_C, Vector3 inp_z)
        {
            this.C = inp_C;
            this.z = inp_z;
        }
        //Ray with points equal to  x= C+ tz for real t
        public Vector3 C;
        public Vector3 z;
    }

    public List<Ray> rays;
    public GameObject prefab;
    private GameObject sphere;
    public GameObject ray_prefab;
    private List<GameObject> lines;
    // Start is called before the first frame update
    void Start()
    {
        rays = new List<Ray>();
        lines = new List<GameObject>();
        rays.Add(new Ray(new Vector3(0, 1, 1), new Vector3(1, .0f, .0f)));
        rays.Add(new Ray(new Vector3(1, 0, 0), new Vector3(.0f, 1.0f, -.0f)));
        rays.Add(new Ray(new Vector3(3, 2, 2), new Vector3(.3f, .2f, -.2f)));
        rays.Add(new Ray(new Vector3(1.5f, 1.5f, 1), new Vector3(.2f, .4f, -.0f)));
        rays.Add(new Ray(new Vector3(2, 2, 1), new Vector3(.3f, .1f, -.1f)));
        rays.Add(new Ray(new Vector3(2, 1, 1), new Vector3(.1f, .2f, .1f)));

        foreach ( Ray ray in rays)
        {
            GameObject obj = Instantiate(ray_prefab);
            obj.GetComponent<LineRenderer>().SetPosition(0, ray.C - ray.z * 10);
            obj.GetComponent<LineRenderer>().SetPosition(1, ray.C + ray.z * 10);
            lines.Add(obj);
        }

        Vector3 closest_point = GetClosestPoint();
        sphere = Instantiate(prefab);
        sphere.transform.position = closest_point;

    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < rays.Count; i++)
        {
            Vector3 first_pos = lines[i].GetComponent<LineRenderer>().GetPosition(0) + lines[i].transform.position;
            Vector3 sec_pos = lines[i].GetComponent<LineRenderer>().GetPosition(1) + lines[i].transform.position;
            rays[i].C = first_pos;
            UnityEngine.Debug.Log($"first pos {first_pos}");
            rays[i].z = first_pos - sec_pos;

        }
         Vector3 closest_point = GetClosestPoint();
        UnityEngine.Debug.Log($"Update call {closest_point}");
        sphere.transform.position = closest_point;
    }


    private Vector3 GetClosestPoint()
    {
        //constructing A,b st. Ay = b for closest point y

        Matrix<double> A = DenseMatrix.OfArray(new double[,] {
        {0,0,0},
        {0,0,0},
        {0,0,0}});

        Vector<double> b = Vector<double>.Build.Dense(3);
        
        foreach(Ray ray in rays)
        {
            //convert C and z into vectors
            Vector<double> C_v = Vector<double>.Build.Dense(3, (i)=>ray.C[i]);
            Vector<double> z_v = Vector<double>.Build.Dense(3, (i) => ray.z[i]);
            z_v = z_v.Multiply(1/z_v.Norm(2));
            //add C to b
            b = b.Add(C_v);

            //add identity to A
            A = A.Add(Matrix<double>.Build.DenseIdentity(3));

            //b -= z_v* (C_v*z_v  / Norm(C_v)**2)
            double dot_prod = C_v.DotProduct(z_v);
            double multipl = dot_prod / (z_v.Norm(2) * z_v.Norm(2));

            b= b.Subtract(z_v.Multiply(multipl));

            //A-= z_v matrix * 1/Norm(C_v)**2
            Matrix<double> z_v_matrix = DenseMatrix.OfArray(new double[,] {
                {z_v[0]*z_v[0], z_v[0]*z_v[1], z_v[0]*z_v[2]},
                {z_v[1]*z_v[0], z_v[1]*z_v[1], z_v[1]*z_v[2]},
                {z_v[2]*z_v[0], z_v[2]*z_v[1], z_v[2]*z_v[2]}});
            z_v_matrix = z_v_matrix.Multiply(1 / (z_v.Norm(2) * z_v.Norm(2)));
            A= A.Subtract(z_v_matrix);
        }
        Vector<double> result = A.Inverse().Multiply(b);

        return new Vector3((float)(result[0]), (float)(result[1]), (float)(result[2]));
    }

}
