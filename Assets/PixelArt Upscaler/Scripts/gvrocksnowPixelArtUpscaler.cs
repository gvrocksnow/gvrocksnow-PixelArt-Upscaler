using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SFB;
using System.Linq;
using UnityEngine.UI;


[ExecuteInEditMode]
public class gvrocksnowPixelArtUpscaler : MonoBehaviour {

    public Texture2D inputTexture;
    public int scaleFactor = 2;
    public Color selectInputOutlineColor = Color.black;
    //public Color selectInputBackgroundColor = Color.clear;
    public bool singlePixelOutline = false;
    public bool removeExtraLines = true;
    public bool fillGaps = true;
    public bool tryFixingArtifacts = false;
    public bool fillColors = true;
    public bool interpolateFillColors = true;
    public InterpolationOrder interpolationOrder;
    public string outputNameSuffix = "Output";
    public bool process = false;

    public bool openFolderSelectionPanel = false;
    public bool openFileSelectionPanel = false;

    private Color artifactPixelColorA = Color.green;
    private Color artifactPixelColorB = Color.yellow;
    public List<Color> inputTextureColorList;
    private List<int> extraLinePixels;
    private string fileName;
    private string folderName;

    [Header("RUNTIME UI")]
    public InputField outputSuffixInput;
    public InputField scaleFactorInput;
    public Toggle removeExtraLinesToggle;
    public Toggle fillGapsToggle;
    public Toggle tryFixingArtifactsToggle;
    public Toggle fillColorsToggle;
    public Toggle interpolateFillColorsToggle;
    public Dropdown interpolationOrderDropdown;
    public Dropdown scalingModeDropdown;
    public ColorPicker colorPicker;
    public Text statusText;

    public enum InterpolationOrder
    {
        darkColorsOverLightColors,
        lightColorsOverDarkColors
    }

    // Use this for initialization
    void Start () {
        inputTextureColorList = new List<Color>();
        colorPicker.AssignColor(Color.black);
        statusText.text = "SELECT INPUT IMAGE OR FOLDER";
	}

    public void GetValuesFromUI()
    {
        outputNameSuffix = outputSuffixInput.text;
        int.TryParse(scaleFactorInput.text,out scaleFactor);
        removeExtraLines = removeExtraLinesToggle.isOn;
        fillGaps = fillGapsToggle.isOn;
        tryFixingArtifacts = tryFixingArtifactsToggle.isOn;
        fillColors = fillColorsToggle.isOn;
        interpolateFillColors = interpolateFillColorsToggle.isOn;


        if (scalingModeDropdown.value == 0)
        {
            singlePixelOutline = true;
        }
        else
        {
            singlePixelOutline = false;
        }



        if (interpolationOrderDropdown.value == 0)
        {
            interpolationOrder = InterpolationOrder.darkColorsOverLightColors;
        }
        if (interpolationOrderDropdown.value == 1)
        {
            interpolationOrder = InterpolationOrder.lightColorsOverDarkColors;
        }

        selectInputOutlineColor = colorPicker.CurrentColor;

    }

    // Update is called once per frame
    void Update () {

        if (Application.isPlaying)
        {
            GetValuesFromUI();
        }


        if (openFolderSelectionPanel)
        {
            ProcessFolder();
        }
        else if (openFileSelectionPanel)
        {
            ProcessSingleFile();
        }

        if (process)
        {
            if (!string.IsNullOrEmpty(fileName))
            {

                byte[] inputTextureData = new byte[2];

                inputTextureData = File.ReadAllBytes(fileName);
                inputTexture = new Texture2D(2, 2);
                inputTexture.name = Path.GetFileNameWithoutExtension(fileName);
                inputTexture.LoadImage(inputTextureData);

                //string path = UnityEditor.AssetDatabase.GetAssetPath(someTexture);
                //UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter; 
                //importer.isReadable = true;
                //importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                //importer.filterMode = FilterMode.Point;
                //UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
                //UnityEngine.Profiling.Profiler.enabled = true;
                //UnityEngine.Profiling.Profiler.BeginSample("gvrocksnow's Pixel Art Processor");
                ProcessImage(Path.GetDirectoryName(fileName));
                //UnityEngine.Profiling.Profiler.EndSample();
                //UnityEngine.Profiling.Profiler.enabled = false;

                statusText.text = "SUCCEEDED UPSCALING IMAGE : " + Path.GetFileName(fileName);
                fileName = null;
            }

            if (!string.IsNullOrEmpty(folderName))
            {

                string[] Files = Directory.GetFiles(folderName);

                foreach (string childFile in Files)
                {

                    //if (!childFile.Contains(outputNameSuffix))
                    {
                        byte[] inputTextureData = new byte[2];

                        inputTextureData = File.ReadAllBytes(childFile);
                        inputTexture = new Texture2D(2, 2);
                        inputTexture.name = Path.GetFileNameWithoutExtension(childFile);
                        inputTexture.LoadImage(inputTextureData);

                        ProcessImage(Path.GetDirectoryName(childFile));
                    }
                }
                statusText.text = "SUCCEEDED UPSCALING IMAGES IN FOLDER : " + Path.GetDirectoryName(folderName);
                folderName = null;
            }
        }

        
	}


   

    public void ProcessSingleFile()
    {
        fileName = OpenFileSelectionPanel()[0];
        process = true;
    }

    public void ProcessFolder()
    {
        folderName = OpenFolderSelectionPanel()[0];
        process = true;
    }


    private string[] OpenFolderSelectionPanel()
    {
        openFolderSelectionPanel = false;

        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select input images folder","",false);
        return paths;

    }


    private string[] OpenFileSelectionPanel()
    {
        openFileSelectionPanel = false;
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select input image", "","",false);//
        return paths;
    }

    public void DrawLine(int x1,int x2,int y1, int y2,int texWidth,Color[] colors)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;

        for(int x= x1;x <= x2; x++)
        {
            int y = y1 + (dy * (x - x1) / dx);

            colors[(y * texWidth) + x] = selectInputOutlineColor;
        }

    }

    public void ProcessImage(string folderPath)
    {
        Color[] inputColors = inputTexture.GetPixels();
        Color[] outputColors = new Color[inputColors.Length * (scaleFactor) * (scaleFactor)];

        for (int i = 0; i < outputColors.Length; i++)
        {
            outputColors[i] = new Color(1, 1, 1, 0);
        }

        List<int> outputFromSingleInputPixel = new List<int>();


        if (singlePixelOutline)
        {
            for (int w = 0; w < inputTexture.width; w++)
            {
                for (int h = 0; h < inputTexture.height; h++)
                {

                    int currPxl = h * inputTexture.width + w;
                    Color currPxlColor = inputColors[currPxl];


                    int bottomLeftPixel = -10;
                    int bottomMidPixel = -10;
                    int bottomRightPixel = -10;

                    int leftPixel = -10;
                    int rightPixel = -10;

                    int topLeftPixel = -10;
                    int topMidPixel = -10;
                    int topRightPixel = -10;

                    //if (h > 0 && w > 0 && h < inputTexture.height - 1 && w < inputTexture.width - 1)
                    {
                        if (h > 0 && w > 0)
                        {
                            bottomLeftPixel = (h - 1) * inputTexture.width + (w - 1);
                        }
                        if (h > 0)
                        {
                            bottomMidPixel = (h - 1) * inputTexture.width + (w - 0);
                        }
                        if (h > 0 && w < inputTexture.width - 1)
                        {
                            bottomRightPixel = (h - 1) * inputTexture.width + (w + 1);
                        }

                        if (w > 0)
                        {
                            leftPixel = (h - 0) * inputTexture.width + (w - 1);
                        }

                        if (w < inputTexture.width - 1)
                        {
                            rightPixel = (h - 0) * inputTexture.width + (w + 1);
                        }

                        if (h < inputTexture.height - 1 && w > 0)
                        {

                            topLeftPixel = (h + 1) * inputTexture.width + (w - 1);
                        }

                        if (h < inputTexture.height - 1)
                        {
                            topMidPixel = (h + 1) * inputTexture.width + (w - 0);
                        }

                        if (h < inputTexture.height - 1 && w < inputTexture.width - 1)
                        {
                            topRightPixel = (h + 1) * inputTexture.width + (w + 1);
                        }



                        if (IsPixelOutLineColor(inputColors, currPxl))
                        {


                            //no interpolation scaling
                            /*
                            for (int i = 0; i < scaleFactor; i++)
                            {
                                for (int r = 0; r < scaleFactor; r++) {
                                    //outputColors[(h * inputTexture.width * Mathf.RoundToInt(Mathf.Pow((scaleFactor), 2))) + (w * (scaleFactor)) + ((i) * inputTexture.width * (scaleFactor)) + r] = outlineColor;
                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w  + (i * inputTexture.width)) + r] = outlineColor;

                                }
                            }
                            */

                            //if (IsPixelBlack(topLeftPixel) && IsPixelColored(rightPixel))
                            if (IsPixelOutLineColor(inputColors, bottomLeftPixel))
                            {
                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 0] = outlineColor;

                                for (int a = 0; a < scaleFactor + 1; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((a - 1) * inputTexture.width)) + a - scaleFactor] = selectInputOutlineColor;
                                }
                            }

                            // if (IsPixelBlack(topRightPixel) && IsPixelColored(leftPixel))
                            if (IsPixelOutLineColor(inputColors, bottomRightPixel))
                            {
                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 1] =outlineColor;

                                for (int a = 0; a < scaleFactor + 1; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((a - 1) * inputTexture.width)) + (scaleFactor - a)] = selectInputOutlineColor;
                                }

                            }

                            /*
                            if (IsPixelBlack(topLeftPixel))
                            {
                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 1] = outlineColor;

                                for (int a = 0; a < scaleFactor-1 ; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((a - 1) * inputTexture.width)) + (scaleFactor-a)] = outlineColor;
                                }

                            }
                            */

                            /*
                            //if (IsPixelBlack(topLeftPixel) && IsPixelColored(rightPixel))
                            if (IsPixelBlack(bottomLeftPixel))
                            {
                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 0] = outlineColor;

                                for (int a = 0; a < scaleFactor - 0; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a + scaleFactor - 3) * inputTexture.width))  - a ] = outlineColor;
                                }
                            }


                            //if (IsPixelBlack(topLeftPixel) && IsPixelColored(rightPixel))
                            if (IsPixelBlack(bottomRightPixel))
                            {
                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 0] = outlineColor;

                                for (int a = 0; a < scaleFactor - 0; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a + scaleFactor - 3) * inputTexture.width)) + a] = outlineColor;
                                }
                            }
                            */

                            if (IsPixelOutLineColor(inputColors, topMidPixel))
                            {
                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = outlineColor;
                                for (int a = 0; a < scaleFactor - 1; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a + scaleFactor - 3) * inputTexture.width)) + (0)] = selectInputOutlineColor;
                                }


                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 1] = rightPixel;

                            }


                            if (IsPixelOutLineColor(inputColors, bottomMidPixel))
                            {
                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = outlineColor;
                                for (int a = -1; a < scaleFactor - 0; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (a * inputTexture.width)) + (0)] = selectInputOutlineColor;
                                }


                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 1] = rightPixel;

                            }


                            if (IsPixelOutLineColor(inputColors, leftPixel))
                            {
                                for (int a = 0; a < scaleFactor + 1; a++)
                                {

                                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - 1) * inputTexture.width)) + (a - scaleFactor)] = selectInputOutlineColor;
                                }
                            }


                            if (IsPixelOutLineColor(inputColors, rightPixel))
                            {
                                for (int a = 0; a < scaleFactor + 2; a++)
                                {

                                    //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - 1) * inputTexture.width)) + (scaleFactor-a)] = selectInputOutlineColor;
                                }
                            }


                            //single black pixel
                            if (!IsPixelOutLineColor(inputColors, leftPixel) && !IsPixelOutLineColor(inputColors, rightPixel) && !IsPixelOutLineColor(inputColors, bottomLeftPixel) && !IsPixelOutLineColor(inputColors, bottomRightPixel) && !IsPixelOutLineColor(inputColors, bottomMidPixel) && !IsPixelOutLineColor(inputColors, topMidPixel) && !IsPixelOutLineColor(inputColors, topLeftPixel) && !IsPixelOutLineColor(inputColors, topRightPixel))
                            {

                                for (int i = 0; i < scaleFactor; i++)
                                {
                                    for (int r = 0; r < scaleFactor; r++)
                                    {
                                        //outputColors[(h * inputTexture.width * Mathf.RoundToInt(Mathf.Pow((scaleFactor), 2))) + (w * (scaleFactor)) + ((i) * inputTexture.width * (scaleFactor)) + r] =outlineColor;
                                        outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (i * inputTexture.width)) + r] = selectInputOutlineColor;//
                                        outputFromSingleInputPixel.Add((scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (i * inputTexture.width)) + r);
                                    }
                                }

                            }


                        }
                    }

                    /*
                    else if (IsPixelNotCompletelyTransparent(inputColors, currPxl))
                    {
                        for (int i = 0; i < scaleFactor; i++)
                        {
                            for (int r = 0; r < scaleFactor; r++)
                            {

                                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (i * inputTexture.width)) + r] = currPxl;

                            }
                        }
                    }
                    */

                }
            }



            //remove extra lines
            extraLinePixels = new List<int>();

            if (removeExtraLines)
            {



                if (scaleFactor > 3)
                {
                    for (int p = 0; p < outputColors.Length; p++)
                    {
                        if (HasBlackPixelAtDistance(outputColors, p, scaleFactor - 2, inputTexture.width * scaleFactor)
                            && (HasBlackPixelAtDistance(outputColors, p, 1, inputTexture.width * scaleFactor)))// &&
                                                                                                               // HasBlackPixelAtDistance(outputColors, p, 2, inputTexture.width * scaleFactor)))
                        {
                            //print("entered extra lines loop");
                            //if (!HasColoredPixelsAtDistance(outputColors, p, 2, inputTexture.width * scaleFactor))
                            if (!Has8ColoredPixelsAdjacent(outputColors, p, 1, inputTexture.width * scaleFactor))
                            {


                                bool right = false;
                                bool left = false;
                                bool top = false;
                                bool bottom = false;
                                if (!IsPixelOutLineColor(outputColors, p + 1))
                                {
                                    right = true;
                                }
                                if (!IsPixelOutLineColor(outputColors, p - 1))
                                {
                                    left = true;
                                }
                                if (!IsPixelOutLineColor(outputColors, p + (inputTexture.width * scaleFactor * 1)))
                                {
                                    top = true;
                                }
                                if (!IsPixelOutLineColor(outputColors, p - (inputTexture.width * scaleFactor * 1)))
                                {
                                    bottom = true;
                                }

                                if (top && right)
                                {
                                    for (int s = 0; s <= scaleFactor - 2; s++)
                                    {
                                        //outputColors[p + s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                        extraLinePixels.Add(p + s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                    }
                                }//

                                if (top && left)
                                {
                                    for (int s = 0; s <= scaleFactor - 2; s++)
                                    {
                                        //outputColors[p - s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                        extraLinePixels.Add(p - s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                    }
                                }

                                if (bottom && right)
                                {
                                    for (int s = 0; s <= scaleFactor - 2; s++)
                                    {
                                        //outputColors[p + s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                        extraLinePixels.Add(p + s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);

                                    }
                                }

                                if (bottom && left)
                                {
                                    for (int s = 0; s <= scaleFactor - 2; s++)
                                    {
                                        //outputColors[p - s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                        extraLinePixels.Add(p - s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                    }
                                }

                            }
                        }
                    }


                    List<int> tempPixels = new List<int>();

                    foreach (int e in extraLinePixels)
                    {
                        //if (HasExtraLinePixelAtExactDistanceInAnyOf4Directions(outputColors, e, scaleFactor, inputTexture.width * scaleFactor))

                        if (FoundParallelLinesoFExtraLinePixelsAtExactDistance(outputColors, e, scaleFactor, inputTexture.width * scaleFactor) > 0)
                        {
                            for (int i = 0; i < scaleFactor - 1; i++)
                            {
                                tempPixels.Add(e + i + (i * scaleFactor * inputTexture.width));
                                tempPixels.Add(e + i + (i * scaleFactor * inputTexture.width) + scaleFactor);
                            }
                        }
                        else if (FoundParallelLinesoFExtraLinePixelsAtExactDistance(outputColors, e, scaleFactor, inputTexture.width * scaleFactor) < 0)
                        {
                            for (int i = 0; i < scaleFactor - 1; i++)
                            {
                                tempPixels.Add(e - i + (i * scaleFactor * inputTexture.width));
                                tempPixels.Add(e - i + (i * scaleFactor * inputTexture.width) - scaleFactor);
                            }
                        }
                    }

                    foreach (int b in tempPixels)
                    {
                        extraLinePixels.Remove(b);
                    }

                }
                else if (scaleFactor == 3)
                {

                    List<int> boundedPixels = new List<int>();

                    for (int p = 0; p < outputColors.Length; p++)
                    {
                        if (outputColors[p] != selectInputOutlineColor)
                        {
                            if (IsBoundedBy4BlackPixels(outputColors, p, 1, inputTexture.width * scaleFactor))
                            {

                                if (!Has8ColoredPixelsAdjacent(outputColors, p, 2, inputTexture.width * scaleFactor))
                                {


                                    bool right = false;
                                    bool left = false;
                                    bool top = false;
                                    bool bottom = false;

                                    if (!IsPixelOutLineColor(outputColors, p + 1 + (inputTexture.width * scaleFactor * 1)))
                                    {
                                        right = true;
                                        top = true;
                                    }
                                    if (!IsPixelOutLineColor(outputColors, p - 1 - (inputTexture.width * scaleFactor * 1)))
                                    {
                                        left = true;
                                        bottom = true;
                                    }
                                    if (!IsPixelOutLineColor(outputColors, p - 1 + (inputTexture.width * scaleFactor * 1)))
                                    {
                                        top = true;
                                        left = true;
                                    }
                                    if (!IsPixelOutLineColor(outputColors, p + 1 - (inputTexture.width * scaleFactor * 1)))
                                    {
                                        bottom = true;
                                        right = true;
                                    }

                                    if (top && right)
                                    {
                                        for (int s = 0; s <= scaleFactor - 2; s++)
                                        {
                                            //outputColors[p + s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                            extraLinePixels.Add(p + s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                        }
                                    }//

                                    if (top && left)
                                    {
                                        for (int s = 0; s <= scaleFactor - 2; s++)
                                        {
                                            //outputColors[p - s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                            extraLinePixels.Add(p - s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                        }
                                    }

                                    if (bottom && right)
                                    {
                                        for (int s = 0; s <= scaleFactor - 2; s++)
                                        {
                                            //outputColors[p + s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                            extraLinePixels.Add(p + s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                        }
                                    }

                                    if (bottom && left)
                                    {
                                        for (int s = 0; s <= scaleFactor - 2; s++)
                                        {
                                            //outputColors[p - s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                            extraLinePixels.Add(p - s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                        }
                                    }




                                    /*
                                    if (HasAdjacentColoredPixelsAtDistance(outputColors, p, 3, inputTexture.width * scaleFactor) >= 2)
                                    {
                                        outputColors[p] = outlineColor;
                                        extraLinePixels.Remove(p);
                                    }


                                    if (extraLinePixels.Contains(p))// Color.yellow)
                                    {
                                        if (Has8ColoredPixelsAdjacent(outputColors, p, 2, inputTexture.width * scaleFactor))
                                        {
                                            //outputColors[p] = outlineColor;
                                            extraLinePixels.Remove(p);
                                        }
                                    }
                                    */

                                    //outputColors[p] = Color.blue;
                                }
                                else
                                {
                                    outputColors[p] = selectInputOutlineColor;
                                }
                            }
                        }



                        if (IsSurroundedBy8BlackPixels(outputColors, p, inputTexture.width * scaleFactor))
                        {
                            //outputColors[p] = Color.clear;
                            boundedPixels.Add(p);
                        }



                    }


                    List<int> tempPixels = new List<int>();

                    foreach (int e in extraLinePixels)
                    {
                        if (HasExtraLinePixelAtExactDistanceInAnyOf4Directions(outputColors, e, 3, inputTexture.width * scaleFactor))
                        {
                            tempPixels.Add(e);
                        }
                    }

                    foreach (int b in tempPixels)
                    {
                        extraLinePixels.Remove(b);
                    }


                    foreach (int bp in boundedPixels)
                    {
                        //outputColors[bp] = Color.clear;
                        outputColors[bp] = selectInputOutlineColor;
                    }
                }
                else if (scaleFactor == 2)
                {

                    List<int> boundedPixels = new List<int>();

                    for (int p = 0; p < outputColors.Length; p++)
                    {
                        if (outputColors[p] == selectInputOutlineColor && !outputFromSingleInputPixel.Contains(p))
                        {

                            //if (!Has8ColoredPixelsAdjacent(outputColors, p, 2, inputTexture.width * scaleFactor))
                            //{
                            bool right = false;
                            bool left = false;
                            bool top = false;
                            bool bottom = false;
                            if (!IsPixelNotCompletelyTransparent(outputColors, p + 1))
                            {
                                right = true;
                            }
                            if (!IsPixelNotCompletelyTransparent(outputColors, p - 1))
                            {
                                left = true;
                            }
                            if (!IsPixelNotCompletelyTransparent(outputColors, p + (inputTexture.width * scaleFactor * 1)))
                            {
                                top = true;
                            }
                            if (!IsPixelNotCompletelyTransparent(outputColors, p - (inputTexture.width * scaleFactor * 1)))
                            {
                                bottom = true;
                            }

                            if (top && right && !bottom && !left)
                            {
                                for (int s = 0; s <= scaleFactor - 2; s++)
                                {
                                    //outputColors[p + s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                    extraLinePixels.Add(p + s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                }
                            }//

                            if (top && left && !bottom && !right)
                            {
                                for (int s = 0; s <= scaleFactor - 2; s++)
                                {
                                    //outputColors[p - s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                    extraLinePixels.Add(p - s + (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                }
                            }

                            if (bottom && right && !top && !left)
                            {
                                for (int s = 0; s <= scaleFactor - 2; s++)
                                {
                                    //outputColors[p + s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                    extraLinePixels.Add(p + s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                }
                            }

                            if (bottom && left && !top && !right)
                            {
                                for (int s = 0; s <= scaleFactor - 2; s++)
                                {
                                    //outputColors[p - s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width] = artifactPixelColorB;// Color.yellow;
                                    extraLinePixels.Add(p - s - (scaleFactor - 2 - s) * (scaleFactor) * inputTexture.width);
                                }
                            }

                            if (HasAdjacentColoredPixelsAtDistance(outputColors, p, 2, inputTexture.width * scaleFactor) >= 2)
                            {
                                outputColors[p] = selectInputOutlineColor;
                                extraLinePixels.Remove(p);
                            }


                            if (extraLinePixels.Contains(p))// Color.yellow)
                            {
                                if (Has8ColoredPixelsAdjacent(outputColors, p, 1, inputTexture.width * scaleFactor))
                                {
                                    //outputColors[p] = outlineColor;
                                    extraLinePixels.Remove(p);
                                }
                            }
                            //}
                        }

                        if (IsSurroundedBy8BlackPixels(outputColors, p, inputTexture.width * scaleFactor))
                        {
                            //outputColors[p] = Color.clear;

                            //if (!IsBoundedBy4BlackPixels(outputColors, p, 1, inputTexture.width * scaleFactor))
                            {
                                boundedPixels.Add(p);
                            }


                        }





                    }

                    foreach (int bp in boundedPixels)
                    {

                        //outputColors[bp] = Color.clear;
                        outputColors[bp] = selectInputOutlineColor;



                    }


                }

                //correct marked pixels
                //for(int p = 0; p < outputColors.Length; p++)

                for (int p = 0; p < extraLinePixels.Count; p++)
                {
                    //if(outputColors[p] == artifactPixelColorB)//Color.yellow)
                    {
                        //outputColors[extraLinePixels[p]] = Color.yellow;
                        outputColors[extraLinePixels[p]] = Color.clear;

                    }
                }
            }




            //Fill Gaps
            if (fillGaps)
            {
                for (int p = 0; p < outputColors.Length; p++)
                {
                    //if (IsBoundedBy4BlackPixels(outputColors, p, scaleFactor, inputTexture.width * scaleFactor))
                    if (IsEncapsulatedByBlackPixelsInRectangle(outputColors, p, scaleFactor - 1, inputTexture.width * scaleFactor))
                    {
                        //outputColors[p] = Color.green;
                        outputColors[p] = selectInputOutlineColor;
                    }
                }
            }


            //remove extra black spots/artifacts
            if (tryFixingArtifacts)
            {
                List<int[]> artifactCornerPixelsList = new List<int[]>();

                for (int p = 0; p < outputColors.Length; p++)
                {
                    bool exitLoop = false;
                    int squareOfNBlackPixels = 0;

                    if (IsPixelOutLineColor(outputColors, p))
                    {
                        for (int x = 0; x <= scaleFactor && !exitLoop; x++)
                        {
                            for (int y = 0; y <= scaleFactor && !exitLoop; y++)
                            {
                                //Is it an isolated square of black pixels?
                                bool topSideClear = true;
                                bool bottomSideClear = true;
                                bool leftSideClear = true;
                                bool rightSideClear = true;

                                for (int tp = 1; tp <= scaleFactor - 1; tp++)
                                {
                                    if (IsPixelOutLineColor(outputColors, p + tp + (inputTexture.width * scaleFactor * (scaleFactor + 1))))
                                    {
                                        topSideClear = false;
                                    }
                                }

                                for (int bp = 1; bp <= scaleFactor - 1; bp++)
                                {
                                    if (IsPixelOutLineColor(outputColors, p + bp + (inputTexture.width * scaleFactor * (-1))))
                                    {
                                        bottomSideClear = false;
                                    }
                                }

                                for (int lp = 1; lp <= scaleFactor - 1; lp++)
                                {
                                    if (IsPixelOutLineColor(outputColors, p - 1 + (inputTexture.width * scaleFactor * (lp))))
                                    {
                                        leftSideClear = false;
                                    }
                                }

                                for (int rp = 1; rp <= scaleFactor - 1; rp++)
                                {
                                    if (IsPixelOutLineColor(outputColors, p + (1 + scaleFactor) + (inputTexture.width * scaleFactor * (rp))))
                                    {
                                        rightSideClear = false;
                                    }
                                }


                                if (!topSideClear || !bottomSideClear || !leftSideClear || !rightSideClear)
                                {
                                    exitLoop = true;
                                }
                                else if (IsPixelOutLineColor(outputColors, p + x + (inputTexture.width * scaleFactor * y)))
                                {
                                    squareOfNBlackPixels += 1;
                                }
                                else
                                {
                                    exitLoop = true;
                                }
                            }
                        }
                        if (squareOfNBlackPixels == (scaleFactor + 1) * (scaleFactor + 1))
                        {
                            int[] artifactCornerPixels = new int[4];

                            artifactCornerPixels[0] = p;                                                                    //bottomLeft pixel
                            artifactCornerPixels[1] = p + scaleFactor;                                                      //bottomRight pixel
                            artifactCornerPixels[2] = p + (inputTexture.width * scaleFactor) * scaleFactor;                 //topLeft pixel
                            artifactCornerPixels[3] = p + scaleFactor + (inputTexture.width * scaleFactor) * scaleFactor;   //topRight pixel

                            artifactCornerPixelsList.Add(artifactCornerPixels);
                        }





                    }
                }


                foreach (int[] arr in artifactCornerPixelsList)
                {
                    for (int a = 0; a < arr.Length; a++)
                    {
                        outputColors[arr[a]] = selectInputOutlineColor;// Color.blue;
                    }


                    //sides found by corner adjacent pixels

                    int v = 0;
                    //check vertical lines
                    {
                        if (IsPixelOutLineColor(outputColors, arr[2] + (inputTexture.width * scaleFactor)))
                        {
                            v += 1;
                        }

                        if (IsPixelOutLineColor(outputColors, arr[0] - (inputTexture.width * scaleFactor)))
                        {
                            v += 1;
                        }

                        if (IsPixelOutLineColor(outputColors, arr[1] - (inputTexture.width * scaleFactor)))
                        {
                            v += 1;
                        }

                        if (IsPixelOutLineColor(outputColors, arr[3] + (inputTexture.width * scaleFactor)))
                        {
                            v += 1;
                        }

                    }

                    //add a horizontal gap
                    if (v >= 2)
                    {
                        AddHorizontalGap(outputColors, arr[0]);
                    }

                    int h = 0;

                    //check horizontal lines
                    {
                        if (IsPixelOutLineColor(outputColors, arr[2] - 1))
                        {
                            h += 1;
                        }

                        if (IsPixelOutLineColor(outputColors, arr[0] - 1))
                        {
                            h += 1;
                        }

                        if (IsPixelOutLineColor(outputColors, arr[1] + 1))
                        {
                            h += 1;
                        }

                        if (IsPixelOutLineColor(outputColors, arr[3] + 1))
                        {
                            h += 1;
                        }

                    }

                    //add a vertical gap
                    if (h >= 2)
                    {
                        AddVerticalGap(outputColors, arr[0]);
                    }


                    if (h < 2 && v < 2)
                    {
                        //found backslash diagonal
                        if (IsPixelOutLineColor(outputColors, arr[2] - 1 + (inputTexture.width * scaleFactor)) &&
                            IsPixelOutLineColor(outputColors, arr[1] + 1 - (inputTexture.width * scaleFactor)))
                        {
                            AddBackSlashDiagonal(outputColors, arr[2]);
                        }

                        //found forwardSlash diagonal
                        if (IsPixelOutLineColor(outputColors, arr[0] - 1 - (inputTexture.width * scaleFactor)) &&
                            IsPixelOutLineColor(outputColors, arr[3] + 1 + (inputTexture.width * scaleFactor)))
                        {
                            AddForwardSlashDiagonal(outputColors, arr[0]);
                        }

                        //found diagonal on topleft
                        if (IsPixelOutLineColor(outputColors, arr[2] - 1 + (inputTexture.width * scaleFactor)))
                        {
                            AddBackSlashDiagonal(outputColors, arr[2]);
                        }
                        //found diagonal on topRight
                        if (IsPixelOutLineColor(outputColors, arr[3] + 1 + (inputTexture.width * scaleFactor)))
                        {
                            AddForwardSlashDiagonal(outputColors, arr[0]);
                        }
                        //found diagonal on bottomleft
                        if (IsPixelOutLineColor(outputColors, arr[0] - 1 - (inputTexture.width * scaleFactor)))
                        {
                            AddForwardSlashDiagonal(outputColors, arr[0]);
                        }
                        //found diagonal on bottomRight
                        if (IsPixelOutLineColor(outputColors, arr[1] + 1 - (inputTexture.width * scaleFactor)))
                        {
                            AddBackSlashDiagonal(outputColors, arr[2]);
                        }
                    }


                }




            }


            if (fillColors)
            {
                List<int> errorPixels = new List<int>();
                List<int> filledPixels = new List<int>();
                List<int> doubleFillErrorPixelsList = new List<int>();

                for (int w = 0; w < inputTexture.width; w++)
                {
                    for (int h = 0; h < inputTexture.height; h++)
                    {
                        Color currPxl = inputColors[h * inputTexture.width + w];

                        int range = Mathf.FloorToInt(scaleFactor * 0.5f);

                        int cornerOfFilledPixelList = 0;

                        for (int x = -scaleFactor + 1; x <= scaleFactor - 0; x++)
                        {
                            for (int y = 0; y <= (scaleFactor * 2) - 1; y++)
                            {
                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                bool skipPixel = false;

                                /*
                                int foundBlackPixelToRight = 0;
                                int foundBlackPixelToBottom = 0;

                                for(int i = 1; i < scaleFactor && !skipPixel; i++)
                                {
                                    if(currOutputPixel+i >= 0 && currOutputPixel+i < outputColors.Length && outputColors[currOutputPixel+i] == outlineColor)
                                    {
                                        foundBlackPixelToRight = i;
                                    }
                                    if (currOutputPixel - (i*scaleFactor*inputTexture.width) >= 0 && currOutputPixel - (i * scaleFactor * inputTexture.width) < outputColors.Length && outputColors[currOutputPixel - (i * scaleFactor * inputTexture.width)] == outlineColor)
                                    {
                                        foundBlackPixelToBottom = i;
                                    }
                                }

                                //for()

                                //x += foundBlackPixelToRight;
                                //y -= foundBlackPixelToBottom;

                                //currOutputPixel += (foundBlackPixelToRight - (foundBlackPixelToBottom * inputTexture.width * scaleFactor));
                                */


                                if (!skipPixel && currOutputPixel >= 0 && currOutputPixel < outputColors.Length && currPxl.a != 0 && outputColors[currOutputPixel] != selectInputOutlineColor && currPxl != selectInputOutlineColor)
                                {

                                    if (x <= scaleFactor - 1 && y <= (scaleFactor * 2) - 2)
                                    {
                                        outputColors[currOutputPixel] = currPxl;
                                        //outputColors[currOutputPixel] = Color.white;//
                                        filledPixels.Add(currOutputPixel);
                                    }

                                    if (x == -scaleFactor + 1 && y == 0)
                                    {
                                        cornerOfFilledPixelList = currOutputPixel - (inputTexture.width * scaleFactor);//
                                    }


                                }



                            }
                        }

                        //check if black lines exists between pixels


                        if (BlackLineExistsBetweenCorners(outputColors, cornerOfFilledPixelList, scaleFactor - 1, inputTexture.width * scaleFactor))
                        {
                            //Debug.Log("Yes");
                            doubleFillErrorPixelsList.Add(cornerOfFilledPixelList);

                        }
                        if (BlackLineExistsBetweenCorners(outputColors, cornerOfFilledPixelList + scaleFactor, scaleFactor - 1, inputTexture.width * scaleFactor))
                        {
                            //Debug.Log("Yes");
                            doubleFillErrorPixelsList.Add(cornerOfFilledPixelList + scaleFactor);

                        }
                        if (BlackLineExistsBetweenCorners(outputColors, cornerOfFilledPixelList + (inputTexture.width * scaleFactor * scaleFactor), scaleFactor - 1, inputTexture.width * scaleFactor))
                        {
                            //Debug.Log("Yes");
                            doubleFillErrorPixelsList.Add(cornerOfFilledPixelList + (inputTexture.width * scaleFactor * scaleFactor));

                        }
                        if (BlackLineExistsBetweenCorners(outputColors, cornerOfFilledPixelList + scaleFactor + (inputTexture.width * scaleFactor * scaleFactor), scaleFactor - 1, inputTexture.width * scaleFactor))
                        {
                            //Debug.Log("Yes");
                            doubleFillErrorPixelsList.Add(cornerOfFilledPixelList + scaleFactor + (inputTexture.width * scaleFactor * scaleFactor));

                        }





                    }
                }

                //Correct color bleeded pixels
                foreach (int pixel in doubleFillErrorPixelsList)
                {
                    CorrectFillErrors(outputColors, pixel, scaleFactor, scaleFactor * inputTexture.width);
                    //outputColors[pixel] = Color.red;
                }



                List<int> correctedPixels = new List<int>();
                //correct extra/missing pixels
                foreach (int currOutputPixel in filledPixels)
                {

                    if (IsFillErrorPixel(outputColors, currOutputPixel, inputTexture.width * scaleFactor))
                    {
                        //outputColors[currOutputPixel] = Color.green;
                        //outputColors[currOutputPixel] = Color.clear; 
                        correctedPixels.Add(currOutputPixel);
                    }
                }


                foreach (int currOutputPixel in correctedPixels)
                {
                    outputColors[currOutputPixel] = Color.clear;
                }

            }


            if (fillColors && interpolateFillColors)
            {

                for (int w = 0; w < inputTexture.width; w++)
                {
                    for (int h = 0; h < inputTexture.height; h++)
                    {

                        int currPxl = (h * inputTexture.width) + w;
                        Color currPxlColor = inputColors[currPxl];


                        if (!inputTextureColorList.Contains(currPxlColor))
                        {
                            inputTextureColorList.Add(currPxlColor);
                        }

                    }
                }

                List<Color> orderedColorList = new List<Color>();

                if (interpolationOrder == InterpolationOrder.darkColorsOverLightColors)
                {
                    orderedColorList = inputTextureColorList.OrderByDescending(x => (x.r + x.g + x.b) * x.a).ToList();
                }
                else if (interpolationOrder == InterpolationOrder.lightColorsOverDarkColors)
                {
                    orderedColorList = inputTextureColorList.OrderBy(x => (x.r + x.g + x.b) * x.a).ToList();
                }


                foreach (Color col in orderedColorList)
                {
                    for (int w = 0; w < inputTexture.width; w++)
                    {
                        for (int h = 0; h < inputTexture.height; h++)
                        {

                            int currPxl = (h * inputTexture.width) + w;
                            Color currPxlColor = inputColors[currPxl];

                            //if (SurroundedByNPixelsOfSameColor(inputColors, currPxl, inputTexture.width) <= 8)

                            if (currPxlColor == col)
                            {

                                int range = Mathf.FloorToInt(scaleFactor * 0.5f);

                                //if(scaleFactor%2 == 1)
                                //{
                                //range += 1;
                                //}



                                //same color pixel on top right
                                int topRightPixel = currPxl + 1 + inputTexture.width;
                                if (topRightPixel >= 0 && topRightPixel < inputColors.Length && inputColors[topRightPixel] == currPxlColor && currPxlColor != selectInputOutlineColor)
                                {
                                    int x = 0;

                                    int e = 0;
                                    int f = 0;
                                    int g = 0;

                                    if (scaleFactor % 2 == 1)
                                    {
                                        e = 1;
                                        f = 1;
                                    }

                                    if (scaleFactor == 2)
                                    {
                                        e = 1;// -1;
                                        g = -1;
                                    }

                                    for (int i = 0; i < scaleFactor + e; i++)
                                    {


                                        for (int y = range + g; y < (range * 2) + range + f; y++)
                                        {
                                            x = y - scaleFactor + range + 1; //range + (range-1);

                                            x -= Mathf.CeilToInt(i / 2);

                                            if (i % 2 == 1)
                                            {
                                                x -= 1;
                                            }
                                            int iMod = Mathf.FloorToInt(i * 0.5f);


                                            int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((y + iMod) * inputTexture.width)) + x;

                                            if (currOutputPixel >= 0 && currOutputPixel < outputColors.Length && currPxlColor.a != 0 && outputColors[currOutputPixel] != selectInputOutlineColor && outputColors[currOutputPixel].a != 0)//&& currPxlColor != outlineColor)
                                            {
                                                //if(SurroundedByNPixelsOfSameColor(inputColors,currPxl,inputTexture.width) > 2)
                                                //if(IsColorADarkerThanB(currPxlColor,outputColors[currOutputPixel]))
                                                {
                                                    //outputColors[currOutputPixel] = new Color(currPxlColor.r, currPxlColor.g, currPxlColor.b, 0.9f);
                                                    outputColors[currOutputPixel] = currPxlColor;
                                                }


                                            }

                                        }
                                    }

                                }
                                //same color pixel on top left
                                int topLeftPixel = currPxl - 1 + inputTexture.width;
                                if (topLeftPixel >= 0 && topLeftPixel < inputColors.Length && inputColors[topLeftPixel] == currPxlColor && currPxlColor != selectInputOutlineColor)
                                {
                                    int x = 0;
                                    int e = 0;
                                    int f = 0;

                                    if (scaleFactor % 2 == 1)
                                    {
                                        e = 0;
                                        f = 1;
                                    }

                                    if (scaleFactor == 2)
                                    {
                                        e -= 1;

                                    }

                                    for (int i = e; i < scaleFactor + e; i++)
                                    {
                                        for (int y = range + f; y < (range * 2) + range + f; y++)
                                        {
                                            x = y - scaleFactor + range + 1; //range + (range-1);

                                            if (scaleFactor == 2)
                                            {
                                                x -= 2;
                                            }

                                            x -= Mathf.CeilToInt(i / 2);

                                            if (i % 2 == 1)
                                            {
                                                x -= 1;
                                            }
                                            int iMod = Mathf.FloorToInt(i * 0.5f);


                                            int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((y + iMod) * inputTexture.width)) - x - 2;

                                            if (currOutputPixel >= 0 && currOutputPixel < outputColors.Length && currPxlColor.a != 0 && outputColors[currOutputPixel] != selectInputOutlineColor && outputColors[currOutputPixel].a != 0)//&& currPxlColor != outlineColor)
                                            {
                                                //if (SurroundedByNPixelsOfSameColor(inputColors, currPxl, inputTexture.width) > 2)
                                                //if (IsColorADarkerThanB(currPxlColor, outputColors[currOutputPixel]))
                                                {
                                                    //outputColors[currOutputPixel] = new Color(currPxlColor.r, currPxlColor.g, currPxlColor.b, 0.9f);
                                                    outputColors[currOutputPixel] = currPxlColor;
                                                }
                                            }

                                        }
                                    }

                                }
                            }

                        }

                    }
                }
            }

        }
        else
        {
            

            List<int> filledPixels = new List<int>();

            /*
            //upscale image before interpolation
            for (int w = 0; w < inputTexture.width; w++)
            {
                for (int h = 0; h < inputTexture.height; h++)
                {
                    Color currPxl = inputColors[h * inputTexture.width + w];


                    for (int x = 0; x < scaleFactor; x++)
                    {
                        for (int y = 0; y < scaleFactor; y++)
                        {
                            int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;
                            outputColors[currOutputPixel] = currPxl;
                            filledPixels.Add(currOutputPixel);
                        }
                    }
                }
            }
            */

            if (interpolateFillColors)
            {
                for (int w = 0; w < inputTexture.width; w++)
                {
                    for (int h = 0; h < inputTexture.height; h++)
                    {

                        int currPxl = (h * inputTexture.width) + w;
                        Color currPxlColor = inputColors[currPxl];


                        if (!inputTextureColorList.Contains(currPxlColor))
                        {
                            inputTextureColorList.Add(currPxlColor);
                        }

                    }
                }


                List<Color> orderedColorList = new List<Color>();

                if (interpolationOrder == InterpolationOrder.darkColorsOverLightColors)
                {
                    orderedColorList = inputTextureColorList.OrderByDescending(x => (x.r + x.g + x.b) * x.a).ToList();
                }
                else if (interpolationOrder == InterpolationOrder.lightColorsOverDarkColors)
                {
                    orderedColorList = inputTextureColorList.OrderBy(x => (x.r + x.g + x.b) * x.a).ToList();
                }

                foreach (Color col in orderedColorList)
                {
                    for (int w = 0; w < inputTexture.width; w++)
                    {
                        for (int h = 0; h < inputTexture.height; h++)
                        {

                            int currPxl = (h * inputTexture.width) + w;
                            Color currPxlColor = inputColors[currPxl];

                            //if (SurroundedByNPixelsOfSameColor(inputColors, currPxl, inputTexture.width) <= 8)

                            if (currPxlColor == col)
                            {

                                int range = Mathf.FloorToInt(scaleFactor * 0.5f);

                                //if(scaleFactor%2 == 1)
                                //{
                                //range += 1;
                                //}



                                //same color pixel on top right
                                int topRightPixel = currPxl + 1 + inputTexture.width;
                                if (topRightPixel >= 0 && topRightPixel < inputColors.Length && inputColors[topRightPixel] == currPxlColor)// && currPxlColor != selectInputOutlineColor)
                                {
                                    int x = 0;

                                    int e = 0;
                                    int f = 0;
                                    int g = 0;

                                    if (scaleFactor % 2 == 1)
                                    {
                                        e = 1;
                                        f = 1;
                                    }

                                    if (scaleFactor == 2)
                                    {
                                        e = 1;// -1;
                                        g = -1;
                                    }

                                    for (int i = 0; i < scaleFactor + e; i++)
                                    {


                                        for (int y = range + g; y < (range * 2) + range + f; y++)
                                        {
                                            x = y - scaleFactor + range + 1; //range + (range-1);

                                            x -= Mathf.CeilToInt(i / 2);

                                            if (i % 2 == 1)
                                            {
                                                x -= 1;
                                            }
                                            int iMod = Mathf.FloorToInt(i * 0.5f);


                                            int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((y + iMod) * inputTexture.width)) + x;

                                            currOutputPixel += (scaleFactor* inputTexture.width) + scaleFactor - 1;

                                            if (currOutputPixel >= 0 && currOutputPixel < outputColors.Length)//&& currPxlColor != outlineColor)
                                            {
                                                //if(SurroundedByNPixelsOfSameColor(inputColors,currPxl,inputTexture.width) > 2)
                                                //if(IsColorADarkerThanB(currPxlColor,outputColors[currOutputPixel]))
                                                {
                                                    //outputColors[currOutputPixel] = new Color(currPxlColor.r, currPxlColor.g, currPxlColor.b, 0.9f);
                                                    outputColors[currOutputPixel] = currPxlColor;
                                                }


                                            }

                                        }
                                    }

                                }
                                //same color pixel on top left
                                int topLeftPixel = currPxl - 1 + inputTexture.width;
                                if (topLeftPixel >= 0 && topLeftPixel < inputColors.Length && inputColors[topLeftPixel] == currPxlColor)
                                {
                                    int x = 0;
                                    int e = 0;
                                    int f = 0;

                                    if (scaleFactor % 2 == 1)
                                    {
                                        e = 0;
                                        f = 1;
                                    }

                                    if (scaleFactor == 2)
                                    {
                                        e -= 1;

                                    }

                                    for (int i = e; i < scaleFactor + e; i++)
                                    {
                                        for (int y = range + f; y < (range * 2) + range + f; y++)
                                        {
                                            x = y - scaleFactor + range + 1; //range + (range-1);

                                            if (scaleFactor == 2)
                                            {
                                                x -= 2;
                                            }

                                            x -= Mathf.CeilToInt(i / 2);

                                            if (i % 2 == 1)
                                            {
                                                x -= 1;
                                            }
                                            int iMod = Mathf.FloorToInt(i * 0.5f);


                                            int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((y + iMod) * inputTexture.width)) - x - 2;

                                            currOutputPixel -= (scaleFactor * inputTexture.width) - scaleFactor;
                                            if (currOutputPixel >= 0 && currOutputPixel < outputColors.Length)//&& currPxlColor != outlineColor)
                                            {
                                                //if (SurroundedByNPixelsOfSameColor(inputColors, currPxl, inputTexture.width) > 2)
                                                //if (IsColorADarkerThanB(currPxlColor, outputColors[currOutputPixel]))
                                                {
                                                    //outputColors[currOutputPixel] = new Color(currPxlColor.r, currPxlColor.g, currPxlColor.b, 0.9f);
                                                    outputColors[currOutputPixel] = currPxlColor;
                                                }
                                            }

                                        }
                                    }

                                }

                                //same color pixel on right
                                int rightPixel = currPxl + 1;
                                if (rightPixel >= 0 && rightPixel < inputColors.Length && inputColors[rightPixel] == currPxlColor)
                                {

                                    int xStart = 0;
                                    int xEnd = scaleFactor;
                                    int yStart = 0;
                                    int yEnd = scaleFactor;


                                    if(currPxlColor == selectInputOutlineColor)
                                    {
                                        xStart = scaleFactor - 1;
                                        xEnd = (scaleFactor * 2) - 1;
                                        yStart = Mathf.FloorToInt(scaleFactor / 3f);
                                    }

                                    for (int x = xStart; x < xEnd; x++)
                                    {
                                        for (int y = yStart ; y < yEnd; y++)
                                        {
                                            
                                            int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((y) * inputTexture.width)) + x;

                                           
                                            if (currOutputPixel >= 0 && currOutputPixel < outputColors.Length)//&& currPxlColor != outlineColor)
                                            {
                                                //if (SurroundedByNPixelsOfSameColor(inputColors, currPxl, inputTexture.width) > 2)
                                                //if (IsColorADarkerThanB(currPxlColor, outputColors[currOutputPixel]))
                                                {
                                                    //outputColors[currOutputPixel] = new Color(currPxlColor.r, currPxlColor.g, currPxlColor.b, 0.9f);
                                                    outputColors[currOutputPixel] = currPxlColor;
                                                }
                                            }

                                        }
                                    }

                                }

                                //same color pixel on top
                                int topPixel = currPxl + inputTexture.width;
                                if (topPixel >= 0 && topPixel < inputColors.Length && inputColors[topPixel] == currPxlColor)
                                {


                                    int xStart = 0;
                                    int xEnd = scaleFactor;
                                    int yStart = 0;
                                    int yEnd = scaleFactor;


                                    if (currPxlColor == selectInputOutlineColor)
                                    {
                                        xStart = Mathf.FloorToInt(scaleFactor / 3f);
                                        yEnd = (scaleFactor * 2) - 1;
                                        yStart = scaleFactor - 1;
                                    }

                                    for (int x = xStart; x < xEnd; x++)
                                    {
                                        for (int y = yStart; y < yEnd; y++)
                                        {

                                            int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((y) * inputTexture.width)) + x;


                                            if (currOutputPixel >= 0 && currOutputPixel < outputColors.Length)//&& currPxlColor != outlineColor)
                                            {
                                                //if (SurroundedByNPixelsOfSameColor(inputColors, currPxl, inputTexture.width) > 2)
                                                //if (IsColorADarkerThanB(currPxlColor, outputColors[currOutputPixel]))
                                                {
                                                    //outputColors[currOutputPixel] = new Color(currPxlColor.r, currPxlColor.g, currPxlColor.b, 0.9f);
                                                    outputColors[currOutputPixel] = currPxlColor;
                                                }
                                            }

                                        }
                                    }

                                }
                            }

                        }

                    }
                }

#if TEST               
                foreach (Color col in orderedColorList)
                {
                    for (int w = 0; w < inputTexture.width; w++)
                    {
                        for (int h = 0; h < inputTexture.height; h++)
                        {

                            int currPxl = (h * inputTexture.width) + w;
                            Color currPxlColor = inputColors[currPxl];

                            //if (SurroundedByNPixelsOfSameColor(inputColors, currPxl, inputTexture.width) <= 8)

                            if (currPxlColor == col && col.a != 0)
                            {

                                int range = Mathf.FloorToInt(scaleFactor * 0.5f);

                                //if(scaleFactor%2 == 1)
                                {
                                    //    range += 1;
                                }



                                //same color pixel on top right
                                int topRightPixel = currPxl + 1 + inputTexture.width;
                                int topPixel = currPxl + inputTexture.width;
                                if (topRightPixel >= 0 && topRightPixel < inputColors.Length && inputColors[topRightPixel] == currPxlColor && inputColors[topPixel] != currPxlColor)
                                {

                                    for (int x = scaleFactor; x < scaleFactor * 2; x++)
                                    {
                                        for (int y = 0; y < scaleFactor; y++)
                                        {

                                            if (x <= y + scaleFactor)
                                            {
                                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                                outputColors[currOutputPixel] = currPxlColor;
                                            }
                                        }

                                    }

                                    for (int x = 0; x < scaleFactor; x++)
                                    {
                                        for (int y = scaleFactor; y < scaleFactor * 2; y++)
                                        {

                                            if (y <= x + scaleFactor)
                                            {
                                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                                outputColors[currOutputPixel] = currPxlColor;
                                            }
                                        }

                                    }

                                    for (int x = scaleFactor; x < scaleFactor * 2; x++)
                                    {
                                        for (int y = scaleFactor; y < scaleFactor * 2; y++)
                                        {

                                            if (x <= y + scaleFactor)
                                            {
                                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                                outputColors[currOutputPixel] = currPxlColor;
                                            }
                                        }

                                    }

                                    /*
                                    for (int x = 0; x < scaleFactor; x++)
                                    {
                                        for (int y = 0; y < scaleFactor; y++)
                                        {

                                            if (x <= y )
                                            {
                                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                                //outputColors[currOutputPixel] = currPxlColor;
                                            }
                                        }

                                    }
                                    */

                                }


                                //same color pixel on top left
                                int topleftPixel = currPxl - 1 + inputTexture.width;
                                int topPixelB = currPxl + inputTexture.width;
                                if (topleftPixel >= 0 && topleftPixel < inputColors.Length && inputColors[topleftPixel] == currPxlColor && inputColors[topPixelB] != currPxlColor)
                                {

                                    for (int x = 0; x > -scaleFactor; x--)
                                    {
                                        for (int y = 0; y < scaleFactor; y++)
                                        {

                                            if (x >= -y)
                                            {
                                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                                outputColors[currOutputPixel] = currPxlColor;
                                            }
                                        }

                                    }

                                    
                                    for (int x = 0; x < scaleFactor; x++)
                                    {
                                        for (int y = scaleFactor; y < scaleFactor * 2; y++)
                                        {

                                            if (x + y < scaleFactor*2)
                                            {
                                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                                outputColors[currOutputPixel] = currPxlColor;
                                            }
                                        }

                                    }

                                    
                                    for (int x = 0; x > -scaleFactor; x--)
                                    {
                                        for (int y = scaleFactor; y < scaleFactor * 2; y++)
                                        {

                                            if (x <= scaleFactor - y)
                                            {
                                                int currOutputPixel = (scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (y * inputTexture.width)) + x;

                                                outputColors[currOutputPixel] = currPxlColor;
                                            }
                                        }

                                    }
                                    
                                    
                                }
                            }

                        }

                    }
                }
#endif
            }
        }


        Texture2D outputTexture = new Texture2D(inputTexture.width * (scaleFactor), inputTexture.height * (scaleFactor), TextureFormat.RGBA32, false);

        // Read screen contents into the texture
        outputTexture.SetPixels(outputColors);
        outputTexture.Apply();
        byte[] bytes = outputTexture.EncodeToPNG();

        //File.WriteAllBytes(Application.dataPath + "/" + inputTexture.name + "_" + outputNameSuffix + ".png", bytes);
        File.WriteAllBytes(folderPath + "/" + inputTexture.name + "_" + outputNameSuffix + ".png", bytes);
        print("Succeeded Upscaling : " + inputTexture.name);
       
        process = false;
    }


    public void CorrectFillErrors(Color[] input,int startPixel,int scaleFactor,int texWidth)
    {
        
        //if top pixel is black do this
        if(IsPixelOutLineColor(input,startPixel + texWidth))
        {
            //Debug.Log("Top");
            int topLeftPixel = startPixel + (texWidth * scaleFactor);
            Color correctedColor = input[topLeftPixel];
            

            for(int a = 0; a < scaleFactor; a++)
            {
                for (int b = 0; b < scaleFactor; b++)
                {
                    int currPixel = topLeftPixel + a - (b * texWidth);

                    if (IsPixelOutLineColor(input,currPixel))
                    {
                        break;
                    }
                    else
                    {
                        if (currPixel >= 0 && currPixel < input.Length)
                        {
                            input[currPixel] = correctedColor;
                        }
                    }
                    //
                }
            }

            /*
            int belowStartPixel = startPixel - (texWidth * (scaleFactor-1));
            Color correctedColor2 = selectInputBackgroundColor;// input[belowStartPixel];


            for (int a = 0; a < scaleFactor; a++)
            {
                for (int b = 0; b < scaleFactor; b++)
                {
                    int currPixel = belowStartPixel + a + (b * texWidth);

                    if (IsPixelOutLineColor(input, currPixel))
                    {
                        break;
                    }
                    else
                    {
                        if (currPixel >= 0 && currPixel < input.Length)
                        {
                            input[currPixel] = correctedColor2;
                        }
                    }
                    //
                }
            }
            */


        }
        //else do this
        else
        {
            //Debug.Log("Bottom");
            int bottomLeftPixel = startPixel;
            Color correctedColor = input[bottomLeftPixel];


            for (int a = 0; a < scaleFactor; a++)
            {
                for (int b = 0; b < scaleFactor; b++)
                {
                    int currPixel = bottomLeftPixel + a + (b * texWidth);

                    if (IsPixelOutLineColor(input,currPixel))
                    {
                        break;
                    }
                    else
                    {
                        if (currPixel >= 0 && currPixel < input.Length)
                        {
                            input[currPixel] = correctedColor;
                        }
                    }
                    //
                }
            }

            
            int aStartPixel = startPixel;// - (texWidth * scaleFactor);
            Color correctedColor2 = input[aStartPixel];


            for (int a = 0; a < scaleFactor; a++)
            {
                for (int b = 0; b < scaleFactor; b++)
                {
                    int currPixel = aStartPixel + a - (b * texWidth);

                    if (IsPixelOutLineColor(input, currPixel))
                    {
                        break;
                    }
                    else
                    {
                        if (currPixel >= 0 && currPixel < input.Length)
                        {
                            input[currPixel] = correctedColor2;
                        }
                    }
                    
                }
            }
            
        }


    }

    public bool BlackLineExistsBetweenCorners(Color[] input,int bottomLeft, int inScaleFactor,int texWidth)
    {

        int range = inScaleFactor;

        int start = 1;

        bool output = false;

        bool showDebugSquares = false;

        if(IsPixelOutLineColor(input,bottomLeft))// ||  (bottomLeft >= 0 && bottomLeft<input.Length && input[bottomLeft] == selectInputBackgroundColor))//input[bottomLeft].a == 0))
        {
            return false;
        }

        //check from bottom left to bottom right
        for(int i = start; i < range+1; i++)
        {
            int p = bottomLeft + i;

            if (showDebugSquares && p >= 0 && p < input.Length)
            {
                input[p] = Color.green;
            }

            if (IsPixelOutLineColor(input,p))
            {
                if (showDebugSquares)
                {
                    output = true;
                }
                else
                {
                    return true;
                }
                
            }
        }
        //check from bottom left to top left
        for (int i = start; i < range+1; i++)
        {
            int p = bottomLeft + (i * texWidth);

            if (showDebugSquares && p >= 0 && p < input.Length)
            {
                input[p] = Color.green;
            }

            if (IsPixelOutLineColor(input,p))
            {
                if (showDebugSquares)
                {
                    output = true;
                }
                else
                {
                    return true;
                }
            }
        }
        //check from top right to bottom right
        for (int i = start; i < range; i++)
        {
           
            int p = bottomLeft + range + (range*texWidth) - (i * texWidth);

            if (showDebugSquares && p >= 0 && p < input.Length)
            {
                input[p] = Color.green;
            }

            if (IsPixelOutLineColor(input,p))
            {
                if (showDebugSquares)
                {
                    output = true;
                }
                else
                {
                    return true;
                }
            }
        }
        //check from top right to top left
        for (int i = start; i < range; i++)
        {
            int p = bottomLeft + range + (range * texWidth) - (i);

            if (showDebugSquares && p >= 0 && p < input.Length)
            {
                input[p] = Color.green;
            }

            if (IsPixelOutLineColor(input,p))
            {
                if (showDebugSquares)
                {
                    output = true;
                }
                else
                {
                    return true;
                }
            }
        }

        if (output == true)
        {
            return true;
        }
        else
        {
            return false;
        }

        
    }

    public bool IsColorADarkerThanB(Color A,Color B)
    {

        float sumA = A.r + A.g + A.b + A.a;
        float sumB = B.r + B.g + B.b + B.a;

        if(sumA < sumB)
        {
            return true;
        }
        else
        {
            return false;
        }


    }

    public void AddBackSlashDiagonal(Color[] input,int topLeftPixel)
    {

        for(int x= 0; x< scaleFactor; x++)
        {
            for(int y = 0; y < scaleFactor; y++)
            {
                input[topLeftPixel + x - (scaleFactor * inputTexture.width * y)] = Color.clear; //Color.green;
            }
        }

        for(int i = 0; i <= scaleFactor; i++)
        {
            input[topLeftPixel + i - (scaleFactor * inputTexture.width * i)] = selectInputOutlineColor; //Color.green;
        }
    }
    

    public void AddForwardSlashDiagonal(Color[] input, int bottomLeftPixel)
    {

        for (int x = 0; x < scaleFactor; x++)
        {
            for (int y = 0; y < scaleFactor; y++)
            {
                input[bottomLeftPixel + x + (scaleFactor * inputTexture.width * y)] = Color.clear; //Color.green;
            }
        }

        for (int i = 0; i <= scaleFactor; i++)
        {
            input[bottomLeftPixel + i + (scaleFactor * inputTexture.width * i)] = selectInputOutlineColor; //Color.green;
        }
    }

    public void AddHorizontalGap(Color[] input, int bottomLeftPixel)
    {
        for(int x = 1;x < scaleFactor; x++)
        {
            for(int y = 0; y <= scaleFactor; y++)
            {
                input[bottomLeftPixel + x + (scaleFactor * inputTexture.width * y)] = Color.clear;// Color.green;
            }
        }

    }

    public int SurroundedByNPixelsOfSameColor(Color[] input,int pixel,int texWidth)
    {
        int surroundCount = 0;

        Color bottomLeftPixel = Color.clear;
        Color bottomMidPixel = Color.clear;
        Color bottomRightPixel = Color.clear;

        Color leftPixel = Color.clear;
        Color rightPixel = Color.clear;

        Color topLeftPixel = Color.clear;
        Color topMidPixel = Color.clear;
        Color topRightPixel = Color.clear;



        int pix = ((pixel - 1) * texWidth) + (pixel - 1);

        if(pix >= 0 && pix < input.Length)
        {
            bottomLeftPixel = input[pix];
        }
        
        pix = ((pixel - 1) * texWidth) + (pixel - 0);
        if (pix >= 0 && pix < input.Length)
        {
            bottomMidPixel = input[pix];
        }

        pix = ((pixel - 1) * texWidth) + (pixel + 1);
        if (pix >= 0 && pix < input.Length)
        {
            bottomRightPixel = input[pix];
        }

        pix = ((pixel - 0) * texWidth) + (pixel - 1);
        if (pix >= 0 && pix < input.Length)
        {
            leftPixel = input[pix];
        }

        pix = ((pixel - 0) * texWidth) + (pixel + 1);
        if (pix >= 0 && pix < input.Length)
        {
            rightPixel = input[pix];
        }

        pix = ((pixel + 1) * texWidth) + (pixel - 1);
        if (pix >= 0 && pix < input.Length)
        {
            topLeftPixel = input[pix];
        }

        pix = ((pixel + 1) * texWidth) + (pixel - 0);
        if (pix >= 0 && pix < input.Length)
        {
            topMidPixel = input[pix];
        }

        pix = ((pixel + 1) * texWidth) + (pixel + 1);
        if (pix >= 0 && pix < input.Length)
        {
            topRightPixel = input[pix];
        }




        if (bottomLeftPixel == input[pixel])
        {
            surroundCount += 1;
        }
        if (bottomMidPixel == input[pixel])
        {
            surroundCount += 1;
        }
        if (bottomRightPixel == input[pixel])
        {
            surroundCount += 1;
        }
        if (leftPixel == input[pixel])
        {
            surroundCount += 1;
        }
        if (rightPixel == input[pixel])
        {
            surroundCount += 1;
        }
        if (topLeftPixel == input[pixel])
        {
            surroundCount += 1;
        }
        if (topMidPixel == input[pixel])
        {
            surroundCount += 1;
        }
        if (topRightPixel == input[pixel])
        {
            surroundCount += 1;
        }

        if (surroundCount > 0)
        {
            //Debug.Log(surroundCount);
        }


        return surroundCount;
               
    }

    public void AddVerticalGap(Color[] input, int bottomLeftPixel)
    {
        for (int x = 0; x <= scaleFactor; x++)
        {
            for (int y = 1; y < scaleFactor; y++)
            {
                input[bottomLeftPixel + x + (scaleFactor * inputTexture.width * y)] = Color.clear;// Color.green;
            }
        }
    }


    public bool HasAdjacentDiagonalLine(Color[] input, int pixel, int distance, int texWidth)
    {

        for(int r = 1; r <= distance; r++)
        {
           // if(pixel+r+r*texWidth < input.Length && Is)
        }


        return false;
    }

    public int HasNColoredPixelsAdjacent(Color[] input, int pixel, int dist, int texWidth)
    {

        int n = 0;

        if (pixel + 1 + (texWidth * 1) < input.Length && HasAdjacentColoredPixelsAtDistance(input, pixel + 1 + (texWidth * 1), dist, texWidth) == 4)
        {
            n += 1;
        }
        if (pixel - 1 - (texWidth * 1) >= 0 && HasAdjacentColoredPixelsAtDistance(input, pixel - 1 - (texWidth * 1), dist, texWidth) == 4)
        {
            n += 1;
        }
        if (pixel + 1 - (texWidth * 1) >= 0 && HasAdjacentColoredPixelsAtDistance(input, pixel + 1 - (texWidth * 1), dist, texWidth) == 4)
        {
            n += 1;
        }
        if (pixel - 1 + (texWidth * 1) < input.Length && HasAdjacentColoredPixelsAtDistance(input, pixel - 1 + (texWidth * 1), dist, texWidth) == 4)
        {
            n += 1;
        }
        return n;
    }


    public bool IsFillErrorPixel(Color[] input,int pixel,int texWidth)
    {

        if (HasAdjacentTransparentPixelsAtDistance(input, pixel, scaleFactor - 2, texWidth) == 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

   

    public bool Has8ColoredPixelsAdjacent(Color[] input, int pixel,int dist,int texWidth)
    {
       


        if (pixel + 1 + (texWidth * 1) < input.Length && HasAdjacentColoredPixelsAtDistance(input,pixel+1 + (texWidth * 1), dist, texWidth) == 4)
        {
            return true;
        }
        if (pixel - 1 -(texWidth * 1) >= 0 && HasAdjacentColoredPixelsAtDistance(input, pixel - 1 -(texWidth * 1), dist, texWidth) == 4)
        {
            return true;
        }
        if (pixel + 1 - (texWidth * 1) >=0 && HasAdjacentColoredPixelsAtDistance(input, pixel + 1 - (texWidth * 1), dist, texWidth) == 4)
        {
            return true;
        }
        if (pixel - 1 + (texWidth * 1) < input.Length && HasAdjacentColoredPixelsAtDistance(input, pixel - 1 + (texWidth * 1), dist, texWidth) == 4)
        {
            return true;
        }
        return false;
    }


    public int HasAdjacentTransparentPixelsAtDistance(Color[] input, int pixel, int distance, int texWidth)
    {
        int foundCount = 0;
        int countRight = 0;
        int countLeft = 0;
        int countTop = 0;
        int countBottom = 0;

        for (int r = 1; r <= distance; r++)
        {
            if (!IsPixelNotCompletelyTransparent(input,pixel + r) && countRight != -1)
            {
                countRight = 1;
            }

            if (IsPixelOutLineColor(input,pixel + r) && countRight == 0)
            {
                countRight = -1;
            }

        }
        for (int r = 1; r <= distance; r++)
        {
            if (!IsPixelNotCompletelyTransparent(input,pixel - r) && countLeft != -1)
            {
                countLeft = 1;
            }
            if (IsPixelOutLineColor(input,pixel - r) && countLeft == 0)
            {
                countLeft = -1;
            }

        }

        for (int r = 1; r <= distance; r++)
        {
            if (!IsPixelNotCompletelyTransparent(input,pixel + (texWidth * r)) && countTop != -1)
            {
                countTop = 1;
            }

            if (IsPixelOutLineColor(input,pixel + (texWidth * r)) && countTop == 0)
            {
                countTop = -1;
            }
        }

        for (int r = 1; r <= distance; r++)
        {
            if (!IsPixelNotCompletelyTransparent(input,pixel - (texWidth * r)) && countBottom != -1)
            {
                countBottom = 1;
            }
            if (IsPixelOutLineColor(input,pixel - (texWidth * r)) && countBottom == 0)
            {
                countBottom = -1;
            }
        }

        if (countRight > 0)
        {
            foundCount += 1;

        }
        if (countLeft > 0)
        {
            foundCount += 1;
        }
        if (countTop > 0)
        {
            foundCount += 1;
        }
        if (countBottom > 0)
        {
            foundCount += 1;
        }
        //print(foundCount);
        /*
        if (foundCount == 2)
        {
            return true;
        }
        else
        {
            return false;
        }
        */
        return foundCount;
    }


    public int HasAdjacentColoredPixelsAtDistance(Color[] input, int pixel, int distance, int texWidth)
    {
        int foundCount = 0;
        int countRight = 0;
        int countLeft = 0;
        int countTop = 0;
        int countBottom = 0;

        for (int r = 1; r <= distance; r++)
        {
            if (IsPixelNotCompletelyTransparent(input,pixel + r))
            {
                countRight += 1;
            }

            if (countRight == distance)
            {
                foundCount += 1;

            }
        }

        for (int r = 1; r <= distance; r++)
        {

            if (IsPixelNotCompletelyTransparent(input,pixel - r))
            {
                countLeft += 1;
            }

            if (countLeft == distance)
            {
                foundCount += 1;
            }

        }

        for (int r = 1; r <= distance; r++)
        {


            if (IsPixelNotCompletelyTransparent(input,pixel + (texWidth * r)))
            {
                countTop += 1;
            }


            if (countTop == distance)
            {
                foundCount += 1;
            }

        }

        for (int r = 1; r <= distance; r++)
        {


            if (IsPixelNotCompletelyTransparent(input,pixel - (texWidth * r)))
            {

                countBottom += 1;
            }


            if (countBottom == distance)
            {
                foundCount += 1;
            }

        }

        //print(foundCount);
        /*
        if (foundCount == 2)
        {
            return true;
        }
        else
        {
            return false;
        }
        */
        return foundCount;
    }
    public int FoundParallelLinesoFExtraLinePixelsAtExactDistance(Color[] input, int pixel, int distance, int texWidth)
    {
        int direction = 0;

        int forwardSlashPixelCount = 0;
        int backwardSlashPixelCount = 0;

        for (int d = 0;d < scaleFactor - 1; d++)
        {
            if(extraLinePixels.Contains(pixel + d + (d*texWidth) + distance))
            {
                forwardSlashPixelCount += 1;
            }
            
        }

        for (int d = 0; d < scaleFactor - 1; d++)
        {
            if (extraLinePixels.Contains(pixel - d + (d * texWidth) - distance))
            {
                backwardSlashPixelCount += 1;
            }

        }

        if (forwardSlashPixelCount == scaleFactor - 1)
        {
            direction = 1;
        }
        else if (backwardSlashPixelCount == scaleFactor - 1)
        {
            direction = -1;
        }
        else
        {
            direction = 0;
        }

        return direction;

    }
    public bool HasExtraLinePixelAtExactDistanceInAnyOf4Directions(Color[] input,int pixel,int distance, int texWidth)
    {

        bool rightSide = false;
        bool leftSide = false;
        bool topSide = false;
        bool bottomSide = false;

        int checkPixel = pixel + distance;

        if (checkPixel >= 0 && checkPixel < input.Length && extraLinePixels.Contains(checkPixel))
        {
            rightSide = true;
        }

        checkPixel = pixel - distance;
        if (checkPixel >= 0 && checkPixel < input.Length && extraLinePixels.Contains(checkPixel))
        {
            leftSide = true;
        }

        checkPixel = pixel + (texWidth * distance);
        if (checkPixel >= 0 && checkPixel < input.Length && extraLinePixels.Contains(checkPixel))
        {
            topSide = true;
        }

        checkPixel = pixel - (texWidth * distance);
        if (checkPixel >= 0 && checkPixel < input.Length && extraLinePixels.Contains(checkPixel))
        {
            bottomSide = true;
        }

        if (rightSide || leftSide || topSide || bottomSide)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsEncapsulatedByBlackPixelsInRectangle(Color[] input, int pixel, int range, int texWidth)
    {
        int countLeft = 0;
        int countRight = 0;
        int countTop = 0;
        int countBottom = 0;

        int foundCount = 0;

        bool rightSide = false;
        bool leftSide = false;
        bool topSide = false;
        bool bottomSide = false;

       
        

        for (int r = 1 ; r <= range; r++)
        {

            if (IsPixelOutLineColor(input,pixel + r))
            {
                rightSide = true;
            }

            if (IsPixelOutLineColor(input,pixel - r))
            {
                leftSide = true;
            }

            if (IsPixelOutLineColor(input,pixel + (texWidth*r)))
            {
                topSide = true;
            }

            if (IsPixelOutLineColor(input,pixel - (texWidth*r)))
            {
                bottomSide = true;
            }
        }


        for (int r = -range + 1; r < range; r++)
        {

            int currPixel = pixel + r + texWidth;

            if (IsPixelOutLineColor(input,currPixel))
            {
                countTop += 1;
            }

            if (countTop >= range && leftSide && rightSide)
            {
                //Debug.Log("Found Encapsulated Top Side");
                topSide = true;
                //input[pixel] = Color.cyan;
                //foundCount += 1;
            }

           

        }

        for (int r = -range + 1; r < range; r++)
        {
            int currPixel = pixel + r - texWidth;

            if(IsPixelOutLineColor(input,currPixel))
            {
                countBottom += 1;
            }

            if (countBottom >= range && leftSide && rightSide)
            {
                //Debug.Log("Found Encapsulated Bottom Side");
                bottomSide = true;
                //input[pixel] = Color.red;
                //foundCount += 1;
            }

        }


        for (int r = -range + 1; r < range; r++)
        {
            int currPixel = pixel + 1 + (texWidth * r);

            if (IsPixelOutLineColor(input,currPixel))
            {
                countRight += 1;
            }

            if (countRight >= range && bottomSide && topSide)
            {
                //Debug.Log("Found Encapsulated Right Side");
                rightSide = true;
                //input[pixel] = Color.green;
                //foundCount += 1;
            }

        }

        for (int r = -range + 1; r < range; r++)
        {
            int currPixel = pixel - 1 + (texWidth * r);

            if (IsPixelOutLineColor(input,currPixel))
            {
                countLeft += 1;
            }

            if (countLeft >= range && bottomSide && topSide)
            {
                //Debug.Log("Found Encapsulated Left Side");
                leftSide = true;
                //input[pixel] = Color.blue;
                //foundCount += 1;
            }

        }

       

        if (topSide && bottomSide && rightSide && leftSide)
        {
            //input[pixel] = Color.blue;
            //Debug.Log("Found Encapsulated Pixel");
            return true;
            
        }
        else
        {
            return false;
        }

       


    }


    public bool HasBlackPixelsAtDistance(Color[] input, int pixel, int distance, int texWidth)
    {
        int foundCount = 0;
        int countRight = 0;
        int countLeft = 0;
        int countTop = 0;
        int countBottom = 0;

        for (int r = 1; r <= distance; r++)
        {
            if (IsPixelOutLineColor(input,pixel + r))
            {
                countRight += 1;
            }      

            if (countRight == distance)
            {
                foundCount += 1;
                
            }
        }

        for (int r = 1; r <= distance; r++)
        {
            
                if (IsPixelOutLineColor(input,pixel - r))
                {
                    countLeft += 1;
                }
            
            if (countLeft == distance)
            {
                foundCount += 1;
            }

        }

        for (int r = 1; r <= distance; r++)
        {

            
                if (IsPixelOutLineColor(input,pixel + (texWidth * r)))
                {
                    countTop += 1;
                }
            

            if (countTop == distance)
            {
                foundCount += 1;
            }

        }

        for (int r = 1; r <= distance; r++)
        {

            
                if (IsPixelOutLineColor(input,pixel - (texWidth * r)))
                {

                    countBottom += 1;
                }
            

            if (countBottom == distance)
            {
                foundCount += 1;
            }

        }

        //print(foundCount);
        if (foundCount == 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool HasBlackPixelAtDistance(Color[] input,int pixel,int distance,int texWidth)
    {
        int foundCount = 0;
        int countRight = 0;
        int countLeft = 0;
        int countTop = 0;
        int countBottom = 0;
        for (int r = 1; r <= distance; r++)
        {
            
            if (r < distance) {
                if (!IsPixelOutLineColor(input,pixel + r)){
                    countRight += 1;
                }

            }
            else
            {
                if (IsPixelOutLineColor(input,pixel + r)){
                    countRight += 1;
                }
                
            }

            if(countRight == distance)
            {
                foundCount += 1;
               // print("wfwwf");
                
            }
                  
        }

        for (int r = 1; r <= distance; r++)
        {
            
            if (r < distance)
            {
                if (!IsPixelOutLineColor(input,pixel - r))
                {
                    countLeft += 1;
                }
            }
            else
            {
                if (IsPixelOutLineColor(input,pixel - r))
                {
                    countLeft += 1;
                }
            }

            if (countLeft == distance)
            {
                foundCount += 1;
            }

        }

        for (int r = 1; r <= distance; r++)
        {
           
            if (r < distance)
            {
                if (!IsPixelOutLineColor(input,pixel + (texWidth * r))) {
                    countTop += 1;
                }

            }
            else
            {
                if (IsPixelOutLineColor(input,pixel + (texWidth * r))) {
                    countTop += 1;
                }
            }

            if (countTop == distance)
            {
                foundCount += 1;
            }

        }

        for (int r = 1; r <= distance; r++)
        {
            
            if (r < distance)
            {
                if (!IsPixelOutLineColor(input,pixel - (texWidth * r))) {
                    countBottom += 1;
                }

            }
            else
            {
                if (IsPixelOutLineColor(input,pixel - (texWidth * r))) {

                    countBottom += 1;
                }
            }

            if (countBottom == distance)
            {
                foundCount += 1;
            }

        }

        //print(foundCount);
        if (foundCount == 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsBoundedBy4BlackPixels(Color[] input,int pixel,int range,int texWidth)
    {
        bool right = false;
        bool left = false;
        bool top = false;
        bool bottom = false;

        
        for(int r = 1; r <= range; r++)
        {
            if(IsPixelOutLineColor(input,pixel + r))
            {
                right = true;
            }
        }
        if (right)
        {
            for (int r = 1; r <= range; r++)
            {
                if (IsPixelOutLineColor(input,pixel - r))
                {
                    left = true;
                }
            }
        }
        if (right && left)
        {
            for (int r = 1; r <= range; r++)
            {
                if (IsPixelOutLineColor(input,pixel + (texWidth * r)))
                {
                    top = true;
                }
            }
        }
        if (right && left && top)
        {
            for (int r = 1; r <= range; r++)
            {
                if (IsPixelOutLineColor(input,pixel - (texWidth * r)))
                {
                    bottom = true;
                }//
            }//
        }


        if (right && left && top && bottom)
        {
            return true;
        }
        else {
            return false;
        }

    }

    public bool IsSurroundedBy8BlackPixels(Color[] input, int pixel, int texWidth)
    {
        //range = 1;

        bool topRight = false;
        bool topLeft = false;
        bool bottomRight = false;
        bool bottomLeft = false;

        if (!IsBoundedBy4BlackPixels(input,pixel,1,texWidth))
        {
            return false;
        }


        if (IsPixelOutLineColor(input,pixel + 1 + texWidth))
        {
            topRight = true;
        }

        if (IsPixelOutLineColor(input,pixel - 1 + texWidth))
        {
            topLeft = true;
        }

        if (IsPixelOutLineColor(input,pixel + 1 - texWidth))
        {
            bottomRight = true;
        }

        if (IsPixelOutLineColor(input,pixel - 1 - texWidth))
        {
            bottomLeft = true;
        }



        if (topRight && topLeft && bottomRight && bottomLeft)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsPixelOutLineColor(Color[] texture,int pixel)
    {
        

        if (pixel >= 0 && pixel < texture.Length)
        {
            Color c = texture[pixel];

            if (c.r == selectInputOutlineColor.r && c.g == selectInputOutlineColor.g && c.b == selectInputOutlineColor.b && c.a == selectInputOutlineColor.a)
            //if(texture[pixel] == selectInputOutlineColor)
            //if(texture[pixel].Equals(selectInputOutlineColor))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

    }

    public bool IsPixelNotCompletelyTransparent(Color[] texture,int pixel)
    {
        if (pixel >= 0 && pixel < texture.Length)
        {
            if (texture[pixel].a > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

    }

    public void OldAlgo()
    {


        Color[] inputColors = inputTexture.GetPixels();
        Color[] outputColors = new Color[inputColors.Length * (scaleFactor) * (scaleFactor)];

        int topLeftPixel = 0;
        int topMidPixel = 0;
        int topRightPixel = 0;

        int leftPixel = 0;
        int rightPixel = 0;

        int bottomLeftPixel = 0;
        int bottomMidPixel = 0;
        int bottomRightPixel = 0;

        int h = 0;
        int w = 0;

        if (IsPixelOutLineColor(inputColors, topLeftPixel))
        {

            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 0] = outlineColor;

            for (int a = 0; a < scaleFactor - 1; a++)
            {

                outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (a * inputTexture.width)) + a] = selectInputOutlineColor;
            }


            outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 1] = inputColors[rightPixel];
        }

        if (IsPixelOutLineColor(inputColors, topRightPixel))
        {
            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 1] = outlineColor;

            for (int a = 0; a < scaleFactor - 1; a++)
            {

                outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (a * inputTexture.width)) + (scaleFactor - a - 1)] = selectInputOutlineColor;
            }

            outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 0] = inputColors[leftPixel];
        }

        if (IsPixelOutLineColor(inputColors, bottomLeftPixel))
        {
            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = outlineColor;
            for (int a = 0; a < scaleFactor - 1; a++)
            {

                outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a - 1) * inputTexture.width)) + (a)] = selectInputOutlineColor;
            }


            outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 1] = inputColors[rightPixel];


        }

        if (IsPixelOutLineColor(inputColors, bottomRightPixel))
        {
            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 1] = outlineColor;

            for (int a = 0; a < scaleFactor - 1; a++)
            {

                outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a - 1) * inputTexture.width)) + (scaleFactor - a - 1)] = selectInputOutlineColor;
            }

            outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = inputColors[leftPixel];
        }

        if (IsPixelOutLineColor(inputColors, topMidPixel))
        {
            /*
            if (IsPixelColored(rightPixel))
            {
                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 0] = outlineColor;

                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((a) * inputTexture.width)) + (a)] = outlineColor;
                }
            }
            if (IsPixelColored(leftPixel))
            {
                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((a) * inputTexture.width)) + (scaleFactor - a-1)] = outlineColor;
                }
                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 1] = outlineColor;
            }
            */

            for (int a = 0; a < scaleFactor - 2; a++)
            {

                outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((a) * inputTexture.width)) + (0)] = selectInputOutlineColor;
            }

            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = bottomMidPixel;
        }
        if (IsPixelOutLineColor(inputColors, bottomMidPixel))
        {
            /*
            if (IsPixelColored(rightPixel))
            {
                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a-1) * inputTexture.width)) + (a)] = outlineColor;
                }
                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = outlineColor;
            }
            if (IsPixelColored(leftPixel))
            {
                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a-1) * inputTexture.width)) + (scaleFactor - a-1)] = outlineColor;
                }
                //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 1] = outlineColor;
            }
            */
            for (int a = 0; a < scaleFactor - 2; a++)
            {

                outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - a) * inputTexture.width)) + (0)] = selectInputOutlineColor;
            }
            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (0 * inputTexture.width)) + 0] = topMidPixel;
        }



        if (IsPixelOutLineColor(inputColors, leftPixel))
        {
            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = outlineColor;

            /*
            if (IsPixelColored(topMidPixel))
            {
                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((1) * inputTexture.width)) + (a)] = outlineColor;
                }
                ;
            }
            else
            {
                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((1 ) * inputTexture.width)) + (scaleFactor - a-1)] = outlineColor;
                }

            }
            */

            if (IsPixelNotCompletelyTransparent(inputColors,topMidPixel))
            {
                for (int a = 0; a < scaleFactor - 2; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - 1) * inputTexture.width)) + (a)] = selectInputOutlineColor;
                }
            }
            else
            {
                for (int a = 0; a < scaleFactor - 2; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - 3) * inputTexture.width)) + (a)] = selectInputOutlineColor;
                }

            }


            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 1] = rightPixel;
        }

        if (IsPixelOutLineColor(inputColors, rightPixel))
        {
            /*
            if (IsPixelColored(topMidPixel))
            {
                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor-a-1) * inputTexture.width)) + (a)] = outlineColor;
                }
                ;
            }
            else
            {
                for (int a = 0; a < scaleFactor-1; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor-a-1) * inputTexture.width)) + (scaleFactor - a-1)] = outlineColor;
                }

            }

            */

            if (IsPixelNotCompletelyTransparent(inputColors, topMidPixel))
            {
                for (int a = 0; a < scaleFactor - 2; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - 1) * inputTexture.width)) + (scaleFactor - a)] = selectInputOutlineColor;
                }
            }
            else
            {
                for (int a = 0; a < scaleFactor - 2; a++)
                {

                    outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + ((scaleFactor - 3) * inputTexture.width)) + (a)] = selectInputOutlineColor;
                }

            }

            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 1] = outlineColor;

            //outputColors[(scaleFactor) * ((h * inputTexture.width * (scaleFactor)) + w + (1 * inputTexture.width)) + 0] = leftPixel;

            //if(IsPixelColored(rightPixel)
        }
    }
}


