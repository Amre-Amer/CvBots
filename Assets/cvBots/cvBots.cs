using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cvBots : MonoBehaviour {
    Vector3 ll;
    Vector3 ul;
    Vector3 lr;
    GameObject camBodyL;
    GameObject camBodyR;
    GameObject pixL;
    GameObject pixR;
    GameObject scrL;
    GameObject scrR;
    GameObject linL;
    GameObject linR;
    Camera camL;
    Camera camR;
    RenderTexture rtL;
    RenderTexture rtR;
    Material matL;
    Material matR;
    GameObject ball;
    GameObject rig;
    GameObject rigOffset;
    GameObject head;
    GameObject parent;
    public float eyeSize = .5f;
    public float ipd = 20;
    public float nearClipPlaneDist = 3;
    public float fov = 100;
    public int pixelRes = 256;
    public float ballSpeed = 2f;
    public float neckSpeed = 4;
    float headPitch;
    float headYaw;
    int iL;
    int jL;
    int iR;
    int jR;
    public string layerName = "picturePlane";
    float timeStart;
    public bool ynStep;
    public bool ynManual;
    [Range(0, 200)]
    public float cameraHeight = 100;
    public float tolColor = .01f;
    bool ynFoundLeft;
    bool ynFoundRight;
    int frameCount;

	void Start () {
        init();
	}
	
    void Update()
    {
        if (ynStep == true)
        {
            if (Time.realtimeSinceStartup - timeStart > 1)
            {
                timeStart = Time.realtimeSinceStartup;
                UpdateOne();
            }
        }
        else
        {
            UpdateOne();
        }
    }

    void updateCamera()
    {
        Vector3 pos = (ball.transform.position + head.transform.position) / 2;
        Camera.main.transform.position = pos + new Vector3(30, cameraHeight, 30);
        Camera.main.transform.LookAt(pos);
    }

    void UpdateOne()
    {
        if (ynManual == false)
        {
            moveBall();
        }
        updateCv();
        updateCamera();
        //
        frameCount++;
    }


    void moveBall()
    {
        if (frameCount == 0) {
            ball.transform.position = new Vector3( -60, 0, 0);
        }
        float pitch = Mathf.Cos(frameCount * Mathf.Deg2Rad);
        ball.transform.Rotate(pitch, 2, 0);
        ball.transform.position += ball.transform.forward * ballSpeed;
    }

    void updateCv()
    {
        updatePixelAndLine(camBodyL, camL, rtL, scrL, pixL, linL, "L");
        updatePixelAndLine(camBodyR, camR, rtR, scrR, pixR, linR, "R");
        //
        if (ynFoundLeft == true && ynFoundRight == true)
        {
            head.GetComponent<Renderer>().material.color = Color.white;
            orientateHead();
        } else {
            head.GetComponent<Renderer>().material.color = Color.grey;
        }
    }

    void orientateHead() {
        float pitch = 0;
        float jAve = (jL + jR) / 2;
        float y = jAve / (float)pixelRes;
        if (y >= 0 && y < .45f)
        {
            pitch = neckSpeed;
        }
        if (y > .55f && y < 1f)
        {
            pitch = -neckSpeed;
        }
        //
        float yaw = 0;
        float iAve = (iL + iR) / 2;
        float x = iAve / (float)pixelRes;
        if (x >= 0 && x < .45f) {
            yaw = -neckSpeed;            
        }
        if (x > .55f && x < 1f) {
            yaw = neckSpeed;
        }
        float smooth = .95f;
        headPitch = headPitch * smooth + pitch * (1 - smooth);
        headYaw = headYaw * smooth + yaw * (1 - smooth);
        rig.transform.Rotate(headPitch, headYaw, -1 * rig.transform.eulerAngles.z);
    }

    void updatePixelAndLine(GameObject camBody, Camera cam, RenderTexture rt, GameObject scr, GameObject pix, GameObject lin, string txt)
    {
        int i = -1;
        int j = -1;
        int bestN = getBestPixel(rt, Color.green);
        if (bestN >= 0)
        {
            i = bestN % pixelRes;
            j = bestN / pixelRes;
            updatePixel(cam, scr, pix, i, j);
            adjustLine(lin, camBody.transform.position, pix.transform.position);
            //
            if (txt == "L")
            {
                iL = i;
                jL = j;
                ynFoundLeft = true;
            }
            else
            {
                iR = i;
                jR = j;
                ynFoundRight = true;
            }
        } else {
            if (txt == "L")
            {
                ynFoundLeft = false;
            }
            else
            {
                ynFoundRight = false;
            }
        }
    }

    void adjustLine(GameObject go, Vector3 p1, Vector3 p2)
    {
        go.transform.position = (p1 + p2) / 2;
        float dist = Vector3.Distance(p1, p2);
        go.transform.localScale = new Vector3(.05f, .05f, dist * 150);
        go.transform.LookAt(p2);
    }

    int getBestPixel(RenderTexture rt, Color col)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(pixelRes, pixelRes, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, pixelRes, pixelRes), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        Color[] pixels = tex.GetPixels(0, 0, pixelRes, pixelRes);
        int bestN = -1;
        float bestDist = 1000;
        for (int n = 0; n < pixels.Length; n++)
        {
            Color c = pixels[n];
            float distColor = Vector3.Distance(new Vector3(c.r, c.g, c.b), new Vector3(col.r, col.g, col.b));
            if (distColor < bestDist || n == 0)
            {
                bestN = n;
                bestDist = distColor;
            }
        }
        if (bestDist > tolColor) {
            bestN = -1;
        }
        return bestN;
    }

    void updatePixel(Camera cam, GameObject scr, GameObject pix, int i, int j)
    {
        float w = scr.transform.localScale.x;
        float h = scr.transform.localScale.y;
        float sw = w / pixelRes;
        float sh = h / pixelRes;
        pix.transform.position = cam.ScreenToWorldPoint(new Vector3(i, j, cam.nearClipPlane + 2));
    }

    void initRenderTextures()
    {
        rtL = new RenderTexture(pixelRes, pixelRes, 24, RenderTextureFormat.ARGB32);
        rtR = new RenderTexture(pixelRes, pixelRes, 24, RenderTextureFormat.ARGB32);
    }

    void initMaterials()
    {
        matL = new Material(Shader.Find("Unlit/Texture"));
        matL.mainTexture = rtL;
        //
        matR = new Material(Shader.Find("Unlit/Texture"));
        matR.mainTexture = rtR;
    }

    void initRigCamera(string txt) 
    {
        // camBody
        GameObject camBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        camBody.name = "camBody" + txt;
        float s = -1;
        if (txt == "R") s = 1;
        camBody.transform.position = new Vector3(s * ipd / 2, 0, 0);
        camBody.transform.localScale = new Vector3(.25f, .25f, .25f);
        hideLayer(camBody);

        // cam
        Camera cam = camBody.AddComponent<Camera>();
        if (txt == "L") {
            cam.targetTexture = rtL;
            cam.targetDisplay = 1;
        } else {
            cam.targetTexture = rtR;
            cam.targetDisplay = 2;
        }
        cam.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = nearClipPlaneDist;
        cam.fieldOfView = fov;

        // scr
        GameObject scr = GameObject.CreatePrimitive(PrimitiveType.Quad);
        scr.name = "scr" + txt;
        if (txt == "L") {
            scr.GetComponent<Renderer>().material = matL;
        } else {
            scr.GetComponent<Renderer>().material = matR;
        }
        ll = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        lr = cam.ScreenToWorldPoint(new Vector3(pixelRes, 0, cam.nearClipPlane));
        ul = cam.ScreenToWorldPoint(new Vector3(0, pixelRes, cam.nearClipPlane));
        scr.transform.position = camBody.transform.position + camBody.transform.forward * cam.nearClipPlane;
        scr.transform.localScale = new Vector3(Vector3.Distance(ll, lr), Vector3.Distance(ll, ul), 1);
        hideLayer(scr);
        //
        GameObject scrBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scrBack.transform.position = camBody.transform.position + camBody.transform.forward * (cam.nearClipPlane + 1);
        scrBack.transform.localScale = scr.transform.localScale;
        scrBack.transform.parent = camBody.transform;
        hideLayer(scrBack);

        // pix
        GameObject pix = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pix.name = "pix" + txt;
        pix.GetComponent<Renderer>().material.color = Color.black;
        float sca = scr.transform.localScale.x * eyeSize;
        pix.transform.localScale = new Vector3(sca, sca, sca);
        pix.transform.position = new Vector3(-5, 0, 25);
        pix.transform.parent = parent.transform;
        hideLayer(pix);

        // lin
        GameObject lin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lin.name = "lin" + txt;
        lin.transform.parent = parent.transform;
        hideLayer(lin);

        // parents
        camBody.transform.parent = rigOffset.transform;
        scr.transform.parent = camBody.transform;

        // assignment
        if (txt == "L") {
            camBodyL = camBody;
            camL = cam;
            scrL = scr;
            pixL = pix;
            linL = lin;
        } else {
            camBodyR = camBody;
            camR = cam;
            scrR = scr;
            pixR = pix;
            linR = lin;
        }
    }

    void initRig() 
    {
        ll = Vector3.zero;
        lr = Vector3.zero;
        ul = Vector3.zero;

        // rig parents
        rig = new GameObject("rig");
        rig.transform.parent = parent.transform;
        rigOffset = new GameObject("rigOffset");
        rigOffset.transform.parent = rig.transform;

        // components
        initRigCamera("L");
        initRigCamera("R");
    }

    void init() 
    {
        initRenderTextures();
        initMaterials();
        //
        parent = new GameObject("parent");
        parent.transform.parent = transform;
        //
        initRig();

        // ball
        ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "ball";
        ball.transform.localScale = new Vector3(2, 2, 2);
        ball.transform.position = new Vector3(-10, 0, 25);
        ball.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
        ball.GetComponent<Renderer>().material.color = Color.green;
        ball.transform.parent = transform;

        // head
        head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "head";
        head.transform.localScale = new Vector3(25, 11.7f, 25);
        head.transform.position = new Vector3(0, 0, -8.3f);
        head.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
        head.GetComponent<Renderer>().material.color = Color.white;
        head.transform.parent = rig.transform;
    }

    void hideLayer(GameObject go)
    {
        go.layer = LayerMask.NameToLayer(layerName);
    }
}
