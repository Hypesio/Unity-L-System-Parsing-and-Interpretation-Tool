using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationGrowth : MonoBehaviour
{
    public VegetationGeneration generator;
    public int startIteration = 1;
    public int maxIteration = 1;
    public int step = 1;
    public float timeBetweenIteration;

    private string originalAxiom;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGrowVegetation();
        }
    }

    void StartGrowVegetation()
    {
        if (String.IsNullOrEmpty(originalAxiom))
        {
            originalAxiom = generator.axiom;
        }
        StartCoroutine(IGrowth());
    }

    IEnumerator IGrowth()
    {
        string actualAxiom = originalAxiom;
        for (int i = startIteration; i < maxIteration; i += step)
        {
            generator.axiom = actualAxiom;
            actualAxiom = generator.ApplyGrammar(actualAxiom, step);
            generator.GenerateMesh(actualAxiom);
            yield return new WaitForSeconds(timeBetweenIteration);
        }
    }
}