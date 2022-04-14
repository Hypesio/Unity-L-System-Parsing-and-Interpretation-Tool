# Unity L-System parsing and interpretation tool

The goal of this project is to have a tool to parse and interprete L-System grammar that is easy to use and combine for any usage.

### Parsing progress : 
- :heavy_check_mark: Simple grammar 
- :heavy_check_mark: Stochastic grammar (probability)
- :heavy_check_mark: Parametrical grammar 
- :heavy_check_mark: Defines variables 
- :heavy_multiplication_x: Context sensitive grammar

## Vegetation Generation and destruction :deciduous_tree: :boom:
The only system actually available with this L-System tool can generate vegetation following the guidelines written in the paper *The Algorithmic Beauty of Plants* written by P. Prusinkiewicz, A. Lindenmayer.

The script allows to easily change rules and parameters. You can load or save preset using scriptables objects and save an object generated as a prefab in one click.

### Improvement coming to vegetation generation 
- :heavy_plus_sign: Include shapes
- :heavy_plus_sign: Textures

#### Generation examples
Simple bush and flower made with simple grammar  |  Trees made with parametrical grammar
:-------------------------:|:-------------------------:
![LSystemSimpleBushAndFlower](https://user-images.githubusercontent.com/47392735/163389813-a0c39662-63bd-4677-9032-1b85d1dd15eb.jpg)  |  ![LSystem4SimpleTrees](https://user-images.githubusercontent.com/47392735/163389843-925a31c2-94c8-4307-8f7e-340ba729236f.jpg)

#### Destruction 
From the vegetation generation a data-tree will be made to represent the data. Each node represent a cylinder and hold triangles and vertex informations. It allows to easily find the node of a specific triangle when we hit the vegetation with a raycast. It doesn't "cut" the vegetation but will break at the closest previous intersection in the vegetation. It offers fast and simple way to break vegetation into pieces. 

Before destruction |  After destruction
:-------------------------:|:-------------------------:
 <img src="https://user-images.githubusercontent.com/47392735/163392054-1f1031e4-085c-4f89-a525-198d2dff4116.jpg" width="200" /> | <img src="https://user-images.githubusercontent.com/47392735/163392063-5bbc6888-e06d-4fa7-8283-7cd0dc086c43.jpg" width="200" />
 
 ## How to use parser for your own scripts
```csharp
    public int nbIteration;
    public string axiom;
    [SerializeField] public Rule[] rules;
    public Define[] defines;
 
    public string GenerateSomething()
    {
       string sentenceGenerated = GrammarInterpretation.ApplyGrammar(rules, defines, axiom, nbIteration);
       // ... Do your stuff with the sentence generated
    }
```
