using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static seri_vector_3;


using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text;
using UnityEngine.UI;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public class extract_and_detect_latest_photo : MonoBehaviour
{
    // Start is called before the first frame update
    private string azure_api_key = "API_KEY";
    private string azure_subscription_id = "SUBSCRIPTION ID";
    private string azure_project_id = "PROJECT ID";
    private string azure_iteration_name = "PROJECT NAME";


    //public GameObject obj_det_prob_textfield;

    bool busy_computing;

    public GameObject debug_textfield;
    public GameObject pic_display;
    public GameObject ray_prefab;
    public GameObject sphere_prefab;

    private List<GameObject> detection_rays;
    private GameObject detection_sphere;

    string path_prefix;

    //class for saving the information of an positive image detection.
    //Meaning the line in format x = C + t*z where C is the camera position and z a vector in direction of the line
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



    private List<Ray> detections;


    [System.Serializable]
    public class BoundingBox_class
    {
        public double left, top, width, height;
    }

    [System.Serializable]
    public class Prediction_class
    {
        public double probability;
        public string tagId;
        public string tagName;
        public BoundingBox_class boundingBox;

    }

    [System.Serializable]
    public class JMessage
    {
        public string id;
        public string project;
        public string iteration;
        public string created;
        public List<Prediction_class> predictions;
    }


    void Start()
    {
        path_prefix = Application.persistentDataPath;
        detection_rays = new List<GameObject>();
        detections = new List<Ray>();
        detection_sphere = Instantiate(sphere_prefab);

        busy_computing = false;

        InvokeRepeating("extract_and_detect_latest_image", 2, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        //only show the sphere
        if (!busy_computing)
        {
            //UnityEngine.Debug.Log("huhu");
            //extract_and_detect_latest_image();
        }
        detection_sphere.transform.position = GetClosestPoint();
    }

    private Vector3 GetClosestPoint()
    {
        //constructing A,b st. Ay = b for closest point y
        if (detections.Count <= 1)
        {
            return new Vector3(0, 0, 0);
        }
        Matrix<double> A = DenseMatrix.OfArray(new double[,] {
        {0,0,0},
        {0,0,0},
        {0,0,0}});

        Vector<double> b = Vector<double>.Build.Dense(3);

        foreach (Ray ray in detections)
        {
            //convert C and z into vectors
            Vector<double> C_v = Vector<double>.Build.Dense(3, (i) => ray.C[i]);
            Vector<double> z_v = Vector<double>.Build.Dense(3, (i) => ray.z[i]);
            z_v = z_v.Multiply(1 / z_v.Norm(2));
            //add C to b
            b = b.Add(C_v);

            //add identity to A
            A = A.Add(Matrix<double>.Build.DenseIdentity(3));

            //b -= z_v* (C_v*z_v  / Norm(C_v)**2)
            double dot_prod = C_v.DotProduct(z_v);
            double multipl = dot_prod / (z_v.Norm(2) * z_v.Norm(2));

            b = b.Subtract(z_v.Multiply(multipl));

            //A-= z_v matrix * 1/Norm(C_v)**2
            Matrix<double> z_v_matrix = DenseMatrix.OfArray(new double[,] {
                {z_v[0]*z_v[0], z_v[0]*z_v[1], z_v[0]*z_v[2]},
                {z_v[1]*z_v[0], z_v[1]*z_v[1], z_v[1]*z_v[2]},
                {z_v[2]*z_v[0], z_v[2]*z_v[1], z_v[2]*z_v[2]}});
            z_v_matrix = z_v_matrix.Multiply(1 / (z_v.Norm(2) * z_v.Norm(2)));
            A = A.Subtract(z_v_matrix);
        }
        Vector<double> result = A.Inverse().Multiply(b);
        return new Vector3((float)(result[0]), (float)(result[1]), (float)(result[2]));
    }

    void extract_and_detect_latest_image()
    {
        busy_computing = true;
        //take the image with the smalles number and remove it afterwards (but only do that if there are at least two images)
        string[] filePaths = Directory.GetFiles($"{path_prefix}/tmp_images");
        //UnityEngine.Debug.Log($"{string.Join(",", filePaths)}");
        //UnityEngine.Debug.Log($"found files: {string.Join(",",filePaths)}");
        int smallest_number = int.MaxValue;
        string remove_1 = $"{path_prefix}/tmp_images\\image_";
        string remove_2 = ".dat";
        int nr_files = 0;
        foreach (string filename in filePaths)
        {
            if (filename.Contains(".meta") || filename.Contains("locations"))
            {
                continue;
            }
            nr_files += 1;
            int index = filename.IndexOf(remove_1);
            string file_nr = filename.Remove(index, remove_1.Length);
            index = file_nr.IndexOf(remove_2);
            file_nr = file_nr.Remove(index, remove_2.Length);
            if (smallest_number > int.Parse(file_nr))
            {
                smallest_number = int.Parse(file_nr);
            }
        }
        if (nr_files < 2)
        {
            busy_computing = false;
            return;
        }

        //load smallest image and remove it from directory
        string sm_filepath = $"{path_prefix}/tmp_images\\image_{smallest_number}.dat";
        byte[] curr_image = File.ReadAllBytes(sm_filepath);
        File.Delete(sm_filepath);

        //run detection
        //TODO: get camera positions also! from the timepoint
        List<seri_vector_3> seri_photo_corner_points;
        string sm_corner_filepath = $"{path_prefix}/tmp_images\\locations_{smallest_number}.dat";
        using (Stream stream = File.Open(sm_corner_filepath, FileMode.Open))
        {
            BinaryFormatter bin = new BinaryFormatter();

            seri_photo_corner_points = (List<seri_vector_3>)bin.Deserialize(stream);

        }
        File.Delete(sm_corner_filepath);

        //convert corner points into List<Vector3> and start detect_image
        List<Vector3> photo_corner_points = new List<Vector3>();
        foreach (seri_vector_3 a in seri_photo_corner_points)
        {
            photo_corner_points.Add(new Vector3(a.x, a.y, a.z));
        }


        DetectObjects(photo_corner_points, curr_image);

        busy_computing = false;
    }



    //object detection and photo capture
    async void DetectObjects(List<Vector3> prior_photo_corner_points, byte[] curr_image)
    {
        /*
        // display the prior photo corner points
        foreach (Vector3 point in prior_photo_corner_points)
        {
            GameObject sp_point = Instantiate(sphere_prefab);
            sp_point.transform.position = point;
        }
        */


        //create a 2D texture from curr_image to diplay the current image in game
        //Texture2D tex = new Texture2D(100, 100);
        //tex.LoadImage(curr_image);
        //setting the texture object from RawImage to the created 2Dtexture
        //pic_display.GetComponent<RawImage>().texture = tex;


        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Prediction-Key", azure_api_key);
        client.DefaultRequestHeaders.Add("Prediction-key", azure_subscription_id);
        var url = $"https://findmountobject-prediction.cognitiveservices.azure.com/customvision/v3.0/Prediction/{azure_project_id}/detect/iterations/{azure_iteration_name}/image";
        //convert the byte list image into ByteArrayContent and sending it to Microsoft
        //response: Bad Request Image Format!!
        //Thank in Advance!
        var content = new ByteArrayContent(curr_image);
        var result = await client.PostAsync(url, content);

        /*
        if (!result.IsSuccessStatusCode)
        {
            throw new Exception(result.ReasonPhrase);
        }
        */
        var body = await result.Content.ReadAsStringAsync();

        JMessage response = JsonUtility.FromJson<JMessage>(body);
        UnityEngine.Debug.Log(body);
        //display the detection prob on a textfield
        debug_textfield.GetComponent<TextMesh>().text = response.predictions[0].probability.ToString();

        //1. Check occurence of mountobject -> at the moment only one object to detect so no need to check\
        //Check if there are any predictions
        if (response.predictions.Count() != 0)
        {
            //suppose that the predictions are sorted after prob value
            //at the moment consider only highest probable prediction
            //current threshold is 75 percent
            if (response.predictions[0].probability >= 0.8)
            {
                //TODO find prediction pixel in image and then project the pixel onto vision
                GetPredictionCoordinates(response.predictions[0], prior_photo_corner_points);
            }
        }


    }

    //calculate the prediction pixel(center of bounding box) and project it onto lens vision
    void GetPredictionCoordinates(Prediction_class prediction, List<Vector3> prior_corners)
    {
        //get boundingbox middle
        double left = prediction.boundingBox.left + prediction.boundingBox.width / 2;
        double top = prediction.boundingBox.top + prediction.boundingBox.height / 2;


        //two 3d points the detected ray goes through:
        Vector3 first_ray_pt = prior_corners[0] + (prior_corners[1] - prior_corners[0]) * (float)left + (prior_corners[3] - prior_corners[0]) * (float)(1 - top);
        Vector3 second_ray_pt = prior_corners[4] + (prior_corners[5] - prior_corners[4]) * (float)left + (prior_corners[7] - prior_corners[4]) * (float)(1 - top);
        //detection_tracker_1.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(x, y, 1f));
        //detection_tracker_2.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(x, y, 1.5f));
        GameObject new_ray = Instantiate(ray_prefab);
        detection_rays.Add(new_ray);
        //UnityEngine.Debug.Log($"number vertices: {detection_rays[detection_rays.Count - 1].GetComponent<LineRenderer>().positionCount}");
        detection_rays[detection_rays.Count - 1].GetComponent<LineRenderer>().SetPosition(0, first_ray_pt - (second_ray_pt-first_ray_pt));
        detection_rays[detection_rays.Count - 1].GetComponent<LineRenderer>().SetPosition(1, second_ray_pt + (second_ray_pt -first_ray_pt)*5);

        bool add = true;
        foreach (Ray detection in detections)
        {
            if (detection.z == second_ray_pt - first_ray_pt)
                add = false;
        }
        if (add)
            detections.Add(new Ray(first_ray_pt, second_ray_pt - first_ray_pt));
    }
}
