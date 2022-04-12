using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InterpretationProgression : MonoBehaviour
{
    public TextMeshPro text;
    public Color doneColor = Color.green;

    private string fullSentence;
    // Start is called before the first frame update
    void Start()
    {
        text.text = "";
    }

    public void SetSentence(string sentence)
    {
        fullSentence = sentence;
        text.text = sentence;
    }

    public void InterpretationProgress(int index)
    {
        for (int i = 0; i < index; i++)
        {
            int meshIndex = text.textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = text.textInfo.characterInfo[i].vertexIndex;

            Color32[] vertexColors = text.textInfo.meshInfo[meshIndex].colors32;
            for (int j = 0; j < 4; j++)
            {
                vertexColors[vertexIndex + j] = doneColor;
            }
        }
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }
}