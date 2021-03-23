using UnityEngine;

/// <summary>
/// this class is to generate and store the input/output fields
/// it's not the ioField!
/// </summary>
public class IoFieldsGenerator : MonoBehaviour {
    [Header("prefabs")]
    [SerializeField] private GameObject inFieldPrefab;
    [SerializeField] private GameObject outFieldPrefab;

    [Header("fields")]
    public StackField StackField;
    public TempField[] TempFields;
    public IoPort[] IoPorts;
    public Crane Crane;

    private void Start() {
        initFields();
        InvokeRepeating(nameof(GenerateInField), Parameters.InFieldGenerationInterval, Parameters.InFieldGenerationInterval);
    }

    private void initFields() {
        for (int i = 0; i < Parameters.InitInFieldNumber; i++) {
            GenerateInField();
        }
    }

    public InField GenerateInField() {
        if (GetComponentsInChildren<InField>().Length > 10) return null;
        var obj = Instantiate(inFieldPrefab);
        var inField = obj.GetComponent<InField>();
        inField.Init(IoPorts, this);
        inField.transform.SetParent(inField.Port.transform);
        inField.enabled = false;
        return inField;
    }

    public OutField GenerateOutField() {
        var obj = Instantiate(outFieldPrefab);
        var outField = obj.GetComponent<OutField>();
        outField.Init(IoPorts, this);
        outField.transform.SetParent(outField.Port.transform);
        outField.enabled = false;
        return outField;
    }
}
