using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   
using UnityEngine.EventSystems;

public class Node
{
    #region Variables
    public Vector2 pos;
    public Vector2 displacedPos;
    public bool isFixed;
    public Vector2 force;
    public Color color;
    public GameObject obj;
    #endregion
}

public class Element
{
    #region Variables
    public (Node, Node) nodePair;
    public Color color;
    public GameObject obj;

    public float youngModulus;
    public float yieldStress;
    public float density;
    public float area;

    public float volume;
    public float stress;
    #endregion

    public float[,] GetLocalMatrix() 
    // find the local stiffness matrix of the element
    {
        // calculate element constants
        Vector2 n1 = nodePair.Item1.pos;
        Vector2 n2 = nodePair.Item2.pos;
        float L = Vector2.Distance(n1, n2);
        float X = (n2.x - n1.x) / L;
        float Y = (n2.y - n1.y) / L;
        float E = youngModulus; float A = area;
        float K = E * A / L;
        volume = A * L;

        // return the stiffness matrix
        //                          n1x    n1y    n2x    n2y
        return new float[4, 4] { { X*X*K, X*Y*K,-X*X*K,-X*Y*K},  // n1x
                                 { X*Y*K, Y*Y*K,-X*Y*K,-Y*Y*K},  // n1y
                                 {-X*X*K,-X*Y*K, X*X*K, X*Y*K},  // n2x
                                 {-X*Y*K,-Y*Y*K, X*Y*K, Y*Y*K}}; // n2y
    }

    public float GetStress() 
    // calcuate elemental stress
    {
        Vector2 n1 = nodePair.Item1.pos;
        Vector2 n2 = nodePair.Item2.pos;
        Vector2 rn1 = nodePair.Item1.displacedPos;
        Vector2 rn2 = nodePair.Item2.displacedPos;
        Vector2 strain = new Vector2((n2.x - n1.x) - (rn2.x - rn1.x), (n2.y - n1.y) - (rn2.y - rn1.y));
        return youngModulus * strain.magnitude;
    }

    public float GetVolume() 
    // get volume of material used in element
    {
        Vector2 n1 = nodePair.Item1.pos;
        Vector2 n2 = nodePair.Item2.pos;
        return area * Vector2.Distance(n1, n2);
    }
}

public class TrussAnalyser2D : MonoBehaviour
{
    #region Variables
    [Header("Nodes")]
    public Sprite nodeSprite;
    public Color nodeColor;
    public Color fixedNodeColor;
    public List<Node> nodes;

    [Header("Elements")]
    public Sprite elementSprite;
    public Color elementColor;
    public Color displacedElementColor;
    public List<Element> elements;

    [Header("Objects")]
    public Dropdown materialSelector;
    public Slider areaSlider;
    public Slider scaleSlider;
    [Space(10)]
    public InputField forceInputx;
    public InputField forceInputy;
    [Space(10)]
    public Text areaText;
    public Text volumeText;
    public Text displacementText;
    public Text stressText;
    public Text breakText;
    [Space(10)]
    public GameObject result;

    [HideInInspector] public bool deformationShown;
    [HideInInspector] public bool stressColorsShown;

    private RectTransform graphEditor;

    private List<float> moduli;
    private List<float> yieldStress;
    private List<float> densities;

    private float[,] F;
    private float[,] K;
    private float[,] U;
     
    private float[,] reducedF;
    private float[,] reducedK;
    private float[,] reducedU;
    #endregion

    void Awake()
    // called at the start, create nodes and elements here
    {
        moduli = new List<float>() { 200000f, 5000f, 180000f, 30000f, 15000f, 10000f, 150000f, 50f }; 
        yieldStress = new List<float>() { 1000000f, 70000f, 790000f, 60000f, 44000f, 248000f, 3460000f, 145000f };

        graphEditor = GetComponent<RectTransform>();
        nodes = new List<Node>(); elements = new List<Element>();

        for (int i = 0; i < 4; i++) { CreateNode(new Vector2(200 + 50*i, 250), false); }
        for (int i = 0; i < 5; i++) { CreateNode(new Vector2(175 + 50*i, 200), false); }
        nodes[8].isFixed = nodes[4].isFixed = true;

        for (int i = 0; i < 3; i++) { CreateElement((nodes[i], nodes[i+1])); }
        for (int i = 4; i < 8; i++) { CreateElement((nodes[i], nodes[i+1])); }
        for (int i = 4; i < 8; i++) { CreateElement((nodes[i], nodes[i-4])); }
        for (int i = 5; i < 9; i++) { CreateElement((nodes[i], nodes[i - 5])); }
        UpdateAll();
    }

    public void CreateNode(Vector2 pos, bool isFixed) 
    // instantiates and draws a node
    {
        // instantiate and update node object
        Node nodeClass = new Node();
        nodes.Add(nodeClass);
        nodeClass.pos = pos;
        nodeClass.isFixed = isFixed;

        // attach a TrussNodeHandler2D script and make an image
        GameObject node = new GameObject("Node", typeof(Image));
        nodeClass.obj = node;
        node.gameObject.AddComponent<TrussNodeHandler2D>();
        node.gameObject.GetComponent<TrussNodeHandler2D>().nodeClass = nodeClass;
        node.transform.SetParent(graphEditor, false);
        node.GetComponent<Image>().sprite = nodeSprite;
        node.GetComponent<Image>().color = nodeClass.color;

        // draw node
        RectTransform rect = node.GetComponent<RectTransform>();
        rect.anchoredPosition = nodeClass.pos;
        rect.sizeDelta = new Vector2(12, 12);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
        rect.SetAsLastSibling();
    }

    public void CreateElement((Node, Node) nodePair) 
    // instantiates and draws an element
    {
        // update and instantiate element object
        Element elementClass = new Element();
        elements.Add(elementClass);
        elementClass.nodePair = nodePair;
        elementClass.color = elementColor;

        // make an image
        GameObject element = new GameObject("Element", typeof(Image));
        elementClass.obj = element;
        element.transform.SetParent(graphEditor, false);
        element.GetComponent<Image>().sprite = elementSprite;
        element.GetComponent<Image>().color = elementColor;

        // calculate positions and angles
        Vector2 pos1 = elementClass.nodePair.Item1.pos;
        Vector2 pos2 = elementClass.nodePair.Item2.pos;
        Vector2 dir = (pos2 - pos1).normalized;
        float dist = Vector2.Distance(pos1, pos2);

        // draw image
        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchoredPosition = pos1 + dir * dist / 2;
        rect.sizeDelta = new Vector2(dist, Mathf.Sqrt(elementClass.area));
        rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
        rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan(dir.y / dir.x) * Mathf.Rad2Deg);
        rect.SetAsFirstSibling();
    }

    public void RemoveNode(Node n)
    // removes a node from the graph
    {
        List<Element> elementsCopy = elements;
        foreach (Element e in elements) if (n == e.nodePair.Item1 || n == e.nodePair.Item2)
        {
            Destroy(e.obj);
            elementsCopy.Remove(e);
        }
        elements = elementsCopy;
        Destroy(n.obj);
        nodes.Remove(n);
    }


    public float[,] GetForceVector() 
    // assembles the nodal forces into a vector
    {
        // adds forces to vector from each node
        float[,] f = new float[nodes.Count * 2, 1];
        foreach (Node n in nodes) { f[2 * nodes.IndexOf(n), 0] = n.force.x; f[2 * nodes.IndexOf(n) + 1, 0] = n.force.y; }
        return f;
    }

    public float[,] GetGlobalMatrix() 
    // assemble all local stiffness matrices into a global matrix
    {
        // initialise matrix
        float[,] k = new float[2 * nodes.Count, 2 * nodes.Count];

        int n1; int n2;
        for (int n = 0; n < nodes.Count; n++)
        {
            // search for elements conatining current node
            for (int e = 0; e < elements.Count; e++) if (nodes[n] == elements[e].nodePair.Item1 || nodes[n] == elements[e].nodePair.Item2)
            {
                // set the indices of the nodes depending on the match thats found
                if (nodes[n] == elements[e].nodePair.Item2)
                {
                    n1 = nodes.IndexOf(elements[e].nodePair.Item2);
                    n2 = nodes.IndexOf(elements[e].nodePair.Item1);
                }
                else
                {
                    n1 = nodes.IndexOf(elements[e].nodePair.Item1);
                    n2 = nodes.IndexOf(elements[e].nodePair.Item2);
                }

                // fetch element stiffness matrix
                float[,] E = elements[e].GetLocalMatrix();

                // the ASCII on the right demonstrates which part
                // of the LSM is being transferred to the GSM
                k[2 * n1, 2 * n1] += E[0, 0];         // # # - - 
                k[2 * n1, 2 * n1 + 1] += E[0, 1];     // # # - -
                k[2 * n1 + 1, 2 * n1] += E[1, 0];     // - - - -
                k[2 * n1 + 1, 2 * n1 + 1] += E[1, 1]; // - - - -

                k[2 * n1, 2 * n2] += E[0, 2];         // - - # #
                k[2 * n1, 2 * n2 + 1] += E[0, 3];     // - - # #
                k[2 * n1 + 1, 2 * n2] += E[1, 2];     // - - - -
                k[2 * n1 + 1, 2 * n2 + 1] += E[1, 3]; // - - - -

                k[2 * n2, 2 * n1] += E[2, 0];         // - - - -
                k[2 * n2, 2 * n1 + 1] += E[2, 1];     // - - - -
                k[2 * n2 + 1, 2 * n1] += E[3, 0];     // # # - -
                k[2 * n2 + 1, 2 * n1 + 1] += E[3, 1]; // # # - -

                k[2 * n2, 2 * n2] += E[2, 2];         // - - - -
                k[2 * n2, 2 * n2 + 1] += E[2, 3];     // - - - -
                k[2 * n2 + 1, 2 * n2] += E[3, 2];     // - - # #
                k[2 * n2 + 1, 2 * n2 + 1] += E[3, 3]; // - - # #
            }
        }
        return k;
    }

    public void ReduceMatrices() 
    // remove DOFs where the node is fixed to reduce computation
    {
        // get the DOF indices of fixed nodes
        List<int> indicesToRemove = new List<int>();
        foreach (Node n in nodes) if (n.isFixed) 
        { 
            indicesToRemove.Add(2 * nodes.IndexOf(n)); 
            indicesToRemove.Add(2 * nodes.IndexOf(n) + 1); 
        }

        // initialise reduced variables
        int size = K.GetLength(0) - indicesToRemove.Count;
        reducedK = new float[size, size];
        reducedF = new float[size, 1];

        // transfer values, skipping over the fixed indices
        for (int i = 0, j = 0; i < K.GetLength(0); i++) if (!indicesToRemove.Contains(i))
        {
            for (int k = 0, u = 0; k < K.GetLength(1); k++) if (!indicesToRemove.Contains(k))
            {
                reducedK[j, u] = K[k, i]; u++;
            }
            reducedF[j, 0] = F[i, 0]; j++;
        }
    }

    public float[,] SolveUsingLU(float[,] matrix, float[,] rightPart) 
    // this code was taken from wikipedia
    // https://en.wikipedia.org/wiki/LU_decomposition
    // I did not write this code, just edited it
    {
        // decomposition of matrix
        int n = matrix.GetLength(0);
        float[,] lu = new float[n, n];
        float sum = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = i; j < n; j++)
            {
                sum = 0;
                for (int k = 0; k < i; k++)
                    sum += lu[i, k] * lu[k, j];
                lu[i, j] = matrix[i, j] - sum;
            }
            for (int j = i + 1; j < n; j++)
            {
                sum = 0;
                for (int k = 0; k < i; k++)
                    sum += lu[j, k] * lu[k, i];
                lu[j, i] = (1 / lu[i, i]) * (matrix[j, i] - sum);
            }
        }

        // lu = L+U-I
        // find solution of Ly = b
        float[,] y = new float[n, 1];
        for (int i = 0; i < n; i++)
        {
            sum = 0;
            for (int k = 0; k < i; k++)
                sum += lu[i, k] * y[k, 0];
            y[i, 0] = rightPart[i, 0] - sum;
        }

        // find solution of Ux = y
        float[,] x = new float[n, 1];
        for (int i = n - 1; i >= 0; i--)
        {
            sum = 0;
            for (int k = i + 1; k < n; k++)
                sum += lu[i, k] * x[k, 0];
            x[i, 0] = (1 / lu[i, i]) * (y[i, 0] - sum);
        }
        return x;
    }

    public float[,] GetDisplacementVector() 
    // produce a displacement vector
    {
        // solve for the reduced displacement vector
        float[,] u = new float[2 * nodes.Count, 1];
        reducedU = SolveUsingLU(reducedK, reducedF);

        // put the values back into the full vector
        int i = 0;
        foreach (Node n in nodes) if (!n.isFixed)
        {
            u[2 * nodes.IndexOf(n), 0] = reducedU[i, 0];
            u[2 * nodes.IndexOf(n) + 1, 0] = reducedU[i + 1, 0];
            i += 2;
        }
        return u;
    }


    public void UpdateMatrices() 
    // refresh the matrices to find the displacement vector
    {
        foreach (Node n in nodes)
        {
            // gather inputs
            float.TryParse(forceInputx.text, out float x);
            float.TryParse(forceInputy.text, out float y);
            n.force = new Vector2(x,y) * 1000f;
        }
        foreach (Element e in elements)
        {
            // gather constants
            e.youngModulus = moduli[materialSelector.value];
            e.yieldStress = yieldStress[materialSelector.value];
            //e.density = densities[materialSelector.value];
            e.area = areaSlider.value;
        }

        // call matrix functions to find a solution
        F = GetForceVector();
        K = GetGlobalMatrix();
        ReduceMatrices();
        U = GetDisplacementVector();
    }

    public void UpdateGraph() 
    // updates elements and nodes of the structure
    {
        foreach (Element e in elements)
        {
            // update color
            e.obj.GetComponent<Image>().color = elementColor;

            // calculate positions and angles
            Vector2 pos1 = e.nodePair.Item1.pos;
            Vector2 pos2 = e.nodePair.Item2.pos;
            Vector2 dir = (pos2 - pos1).normalized;
            float dist = Vector2.Distance(pos1, pos2);

            // draw image
            RectTransform rect = e.obj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos1 + dir * dist / 2;
            rect.sizeDelta = new Vector2(dist, Mathf.Sqrt(e.area));
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
            rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan(dir.y / dir.x) * Mathf.Rad2Deg);
        }
        foreach (Node n in nodes)
        {
            // update colour depending on if the node is fixed
            if (n.isFixed) { n.color = fixedNodeColor; }
            else { n.color = nodeColor; }
            n.obj.GetComponent<Image>().color = n.color;
        }
    }

    public void UpdateDisplacementModel() 
    // draws the displacement model with current values
    {
        if (deformationShown)
        {
            // remove old model
            foreach (Transform child in result.transform) { Destroy(child.gameObject); }

            // updates the nodes' displaced position
            foreach (Node n in nodes) { n.displacedPos = n.pos + new Vector2(U[2 * nodes.IndexOf(n), 0], U[2 * nodes.IndexOf(n) + 1, 0]); }

            bool isBroken = false;
            foreach (Element e in elements)
            {
                GameObject element = new GameObject("Element", typeof(Image));
                element.transform.SetParent(result.transform, false);
                Vector2 pos1 = e.nodePair.Item1.displacedPos;
                Vector2 pos2 = e.nodePair.Item2.displacedPos;

                // calculate direction and distance
                Vector2 dir = (pos2 - pos1).normalized;
                float dist = Vector2.Distance(pos1, pos2);

                // draw UI rectangle
                RectTransform rect = element.GetComponent<RectTransform>();
                rect.anchoredPosition = pos1 + dir * dist / 2;
                rect.sizeDelta = new Vector2(dist, Mathf.Sqrt(e.area));
                rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
                if (dir.x == 0) { rect.localEulerAngles = new Vector3(0, 0, 90); }
                else { rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan(dir.y / dir.x) * Mathf.Rad2Deg); }
                rect.SetAsLastSibling();

                // changes break display and colors depending on ticked boxes
                if (stressColorsShown)
                {
                    float stress = e.GetStress();
                    if (stress > e.yieldStress)
                    {
                        // displays broken beams as dark red
                        breakText.text = "Break!"; isBroken = true;
                        e.color = new Color(0.5f, 0, 0, 1);
                    }
                    else
                    {
                        // interpolates stress to find color
                        float stressRatio = Mathf.InverseLerp(0, e.yieldStress, stress);
                        e.color = new Color(stressRatio, 1 - stressRatio, 0, 1);
                    }
                }
                else { e.color = displacedElementColor; }
                element.GetComponent<Image>().color = e.color;
            }
            if (!isBroken) { breakText.text = ""; }
        }
        result.SetActive(deformationShown);
    }

    public void UpdateDataOutputs() 
    // calculates the values and displays the data on screen
    {
        // calculate data
        float globalStress = 0, globalVolume = 0, totalDisplacement = 0;
        foreach (Element e in elements) { globalStress += e.GetStress(); globalVolume += e.GetVolume(); }
        foreach (float f in U) { totalDisplacement += Mathf.Abs(f); }

        // display data
        areaText.text = "Cross section: " + System.Math.Round(areaSlider.value, 2).ToString() + " cm2";
        volumeText.text = "Total volume: \n" + System.Math.Round(globalVolume, 2).ToString() + " cm3";
        displacementText.text = "Total displacement: \n";
        stressText.text = "Total stress: \n";

        // only show certain values depending on the models on screen
        if (deformationShown)
        {
            displacementText.text = "Total displacement: \n" + System.Math.Round(totalDisplacement, 2).ToString() + " cm";
            if (stressColorsShown) 
            { 
                stressText.text = "Total stress: \n" + System.Math.Round(globalStress, 2).ToString() + " Pa"; 
            }
        }
    }

    public void UpdateAll() 
    // update system, finding the displacement model and data outputs
    { UpdateMatrices(); UpdateGraph(); UpdateDisplacementModel(); UpdateDataOutputs(); }


    public void ToggleDeformation() 
    // toggle the deformation model when the box is ticked
    { deformationShown = !deformationShown; UpdateAll(); }
    
    public void ToggleStressColors() 
    // toggle the colour display when the box is ticked
    { stressColorsShown = !stressColorsShown; UpdateAll(); }
}