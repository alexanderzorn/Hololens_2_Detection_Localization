using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections;
using System.Linq;
using UnityEngine.Windows.WebCam;
using System.Text;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public class object_detection : MonoBehaviour
{
    private string azure_api_key = "148ae84aa9d240fd9247ca0a8cac8ae3";
    private string azure_subscription_id = "25a451e2-f943-4731-bcd2-9ce4849903f3";
    private string azure_project_id = "92f985d3-a891-4667-8d69-eb6a64a3db57";
    private string azure_iteration_name = "Iteration2";

    public GameObject obj_det_prob_textfield;
    public GameObject pic_display;
    private List<GameObject> detection_rays;
    public GameObject ray_prefab;
    private int number = 0;
    private byte[] curr_image;
    private Resolution cameraResolution;
    public GameObject sphere_prefab;
    private GameObject detection_sphere;

    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;

    private List<Ray> detections;

    [System.Serializable]
    public class BoundingBox_class
    {
        public double left, top, width, height;
    }

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


// Start is called before the first frame update
void Start()
    {
        detection_rays = new List<GameObject>();
        detections = new List<Ray>();
        detection_sphere = Instantiate(sphere_prefab);

        //InvokeRepeating("DetectObjects", 10f, 10f);
    }

    // Update is called once per frame
    void Update()
    {
        //only show the sphere
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
    async void DetectObjects()
    {
        //before taking a photo get viewframe points(bottom left and upper right) from one and two meter difference from the camera to get the mathematical description of the line right later on.
        List<Vector3> prior_photo_corner_points = new List<Vector3>();
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 1f)));
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 1f)));
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.scaledPixelHeight, 1f)));
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.scaledPixelHeight, 1f)));
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 2f)));
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 2f)));
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.scaledPixelHeight, 2f)));
        prior_photo_corner_points.Add(Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.scaledPixelHeight, 2f)));

        foreach (Vector3 point in prior_photo_corner_points)
        {
            GameObject sp_point = Instantiate(sphere_prefab);
            sp_point.transform.position = point;

        }


        //Take a picture seems to work. should set curr_image onto the current picture taken by camera
        TakePicture();
        //create a 2D texture from curr_image to diplay the current image in game
        Texture2D tex = new Texture2D(100,100);
        tex.LoadImage(curr_image);
        //setting the texture object from RawImage to the created 2Dtexture
        pic_display.GetComponent<RawImage>().texture = tex;


        //TODO hier noch das mit der await struktur hinbekommen damit das so auch sinn ergibt
        if (curr_image == null)
        {
            UnityEngine.Debug.Log("No image captured");
            return;
        }
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Prediction-Key", azure_api_key);
        client.DefaultRequestHeaders.Add("Prediction-key", azure_subscription_id );
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
        obj_det_prob_textfield.GetComponent<TextMesh>().text = response.predictions[0].probability.ToString();

        //1. Check occurence of mountobject -> at the moment only one object to detect so no need to check\
        //Check if there are any predictions
        if(response.predictions.Count() != 0)
        {
            //suppose that the predictions are sorted after prob value
            //at the moment consider only highest probable prediction
            //current threshold is 75 percent
            if(response.predictions[0].probability >= 0.8)
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
        detection_rays[detection_rays.Count - 1].GetComponent<LineRenderer>().SetPosition(0,  first_ray_pt);
        detection_rays[detection_rays.Count - 1].GetComponent<LineRenderer>().SetPosition(1, second_ray_pt);
       
        bool add = true;
        foreach (Ray detection in detections)
        {
            if (detection.z == second_ray_pt -first_ray_pt)
                add = false;
        }
        if (add)
            detections.Add(new Ray (first_ray_pt, second_ray_pt- first_ray_pt));
    }


    void TakePicture()
    {
        UnityEngine.Debug.Log("Taking a picture....");
        cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        //LogManager.Instance.Debug("Taking picture...: " + cameraResolution.width.ToString() + " x " + cameraResolution.height.ToString());
        //LogManager.Instance.Debug("Taking picture...");


        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;
            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.hologramOpacity = 1.0f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.JPEG;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        UnityEngine.Debug.Log("Picture taken.");
        //if (result.success)
        //LogManager.Instance.Debug("Picture has been successfully taken ");
        //DisplayImage(photoCaptureFrame);
        List<byte> imageBufferAsList = new List<byte>();
        photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferAsList);

        // Deactivate the camera
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        curr_image = imageBufferAsList.ToArray();
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}