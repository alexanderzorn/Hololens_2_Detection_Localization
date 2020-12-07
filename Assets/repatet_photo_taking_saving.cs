using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using UnityEngine.Windows.WebCam;
using UnityEngine.UI;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using static seri_vector_3;
using JetBrains.Annotations;

public class repatet_photo_taking_saving : MonoBehaviour
{
    string path_prefix;
    private Resolution cameraResolution;
    PhotoCapture photoCaptureObject = null;
    private byte[] curr_image;
    int image_nr = 0;
    bool busy_capturing;

    // Start is called before the first frame update
    void Start()
    {
        path_prefix = Application.persistentDataPath;
        busy_capturing = false;
        UnityEngine.Debug.Log($"Persistentdatapath: {path_prefix}");
        if (!Directory.Exists($"{path_prefix}/tmp_images"))
        {
            Directory.CreateDirectory($"{path_prefix}/tmp_images");
        }
        else 
        {
            //delete complete content of tmp_images
            string[] filePaths = Directory.GetFiles($"{path_prefix}/tmp_images");
            foreach (string filename in filePaths)
            {
                File.Delete(filename);
            }

        }
        TakePicture();
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (!busy_capturing)
        {
            busy_capturing = true;
            TakePicture();
            busy_capturing = false;
        }
        */
    }


    void run()
    {
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }

    void TakePicture()
    {
        /*
        if (busy_capturing)
        {
            return;
        }
        busy_capturing = true;
        */
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
                InvokeRepeating("run", 1f, 1f);
            });
        });
        UnityEngine.Debug.Log("2");
        busy_capturing = false;
    }
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        UnityEngine.Debug.Log("Picture taken.");
        image_nr += 1;
        //first get camera data to retrieve the detection ray later on
        List<seri_vector_3> prior_photo_corner_points = new List<seri_vector_3>();
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 1f))));
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 1f))));
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.scaledPixelHeight, 1f))));
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.scaledPixelHeight, 1f))));
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 2f))));
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 2f))));
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.scaledPixelHeight, 2f))));
        prior_photo_corner_points.Add(new seri_vector_3(Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.scaledPixelHeight, 2f))));

        using (Stream stream = File.Open($"{path_prefix}/tmp_images/locations_{image_nr}.dat", FileMode.Create))
        {
            BinaryFormatter bin = new BinaryFormatter();
            bin.Serialize(stream, prior_photo_corner_points);
        }


        //if (result.success)
        //LogManager.Instance.Debug("Picture has been successfully taken ");
        //DisplayImage(photoCaptureFrame);
        List<byte> imageBufferAsList = new List<byte>();
        photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferAsList);

        // Deactivate the camera
       // photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        curr_image = imageBufferAsList.ToArray();

   
        //save the image
        File.WriteAllBytes($"{path_prefix}/tmp_images/image_{image_nr}.dat", curr_image);
        UnityEngine.Debug.Log("1");

    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }


}





/*


using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// [Note: You can paste the Lizard class here] <--

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.WriteLine("s=serialize, r=read:");
            switch (Console.ReadLine())
            {
                case "s":
                    var lizards1 = new List<Lizard>();
                    lizards1.Add(new Lizard("Thorny devil", 1, true));
                    lizards1.Add(new Lizard("Casquehead lizard", 0, false));
                    lizards1.Add(new Lizard("Green iguana", 4, true));
                    lizards1.Add(new Lizard("Blotched blue-tongue lizard", 0, false));
                    lizards1.Add(new Lizard("Gila monster", 1, false));

                    try
                    {
                        using (Stream stream = File.Open("data.bin", FileMode.Create))
                        {
                            BinaryFormatter bin = new BinaryFormatter();
                            bin.Serialize(stream, lizards1);
                        }
                    }
                    catch (IOException)
                    {
                    }
                    break;

                case "r":
                    try
                    {
                        using (Stream stream = File.Open("data.bin", FileMode.Open))
                        {
                            BinaryFormatter bin = new BinaryFormatter();

                            var lizards2 = (List<Lizard>)bin.Deserialize(stream);
                            foreach (Lizard lizard in lizards2)
                            {
                                Console.WriteLine("{0}, {1}, {2}",
                                    lizard.Type,
                                    lizard.Number,
                                    lizard.Healthy);
                            }
                        }
                    }
                    catch (IOException)
                    {
                    }
                    break;
            }
        }
    }
}*/