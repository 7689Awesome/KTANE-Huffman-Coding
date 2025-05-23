using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class HuffmanCoding : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    // Digits for the answer input

    private int[] initialProbabilities;
    private int[] adjustedProbabilities;
    private int[] normalizedProbabilities;
    private int[] digitValues;
    private int[] serialNumberValues;
    private float[] decimalProbabilities;
    private float H;
    private string[] huffmanCodes;
    private float L;
    private float codingEfficiency;
    private int correctAnswer;
    private bool canSubmitAnytime;

    public AudioClip buttonPressSound;
    public AudioClip solveSound;
    public AudioClip strikeSound;

    public KMSelectable[] Buttons;         // Array of buttons for interaction
    public TextMesh[] DisplayProbabilities; // Probability displays
    public TextMesh[] AnswerDigits;



    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    private string IntArrayToString(int[] array)
    {
        if (array == null || array.Length == 0)
        {
            return "";
        }

        string[] strArray = new string[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            strArray[i] = array[i].ToString();
        }

        return string.Join(", ", strArray);
    }

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;

        // Initialize digit values to all zeros
        digitValues = new int[AnswerDigits.Length];

        // Set up button interactions
        SetupButtons();
    }

    void OnDestroy()
    { //Shit you need to do when the bomb ends

    }

    void Activate()
    { //Shit that should happen when the bomb arrives (factory)/Lights turn on


    }

    void Start()
    { //Shit that you calculate, usually a majority if not all of the module
        GenerateRandomProbabilities();
        // Initialize all digit displays to 0
        for (int i = 0; i < AnswerDigits.Length; i++)
        {
            UpdateDigitDisplay(i);
        }
        EdgeworkBasedProbabilities();
        NormalizeProbabilities();
        H = CalculateEntropy();
        HuffmanCodingAlgorithm();
        L = CalculateAverageCodewordLength();
        codingEfficiency = CalculateCodingEfficiency();
        AdditionalRules();
    }

    void Update()
    { //Shit that happens at any point after initialization

    }

    void Solve()
    {
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()
    {
        GetComponent<KMBombModule>().HandleStrike();
    }

    // SUBFUNCTIONS

    void SetupButtons()
    {
        // Set up digit manipulation buttons
        for (int i = 0; i < 8; i++)
        {  // Only process the first 8 buttons (4 digits × 2 buttons each)
           // The variable is actually used in the delegate, so we'll suppress the warning

            int digitIndex = i / 2; // Each digit has an up and down button
            bool isUpButton = i % 2 == 0;

            Buttons[i].OnInteract += delegate ()
            {
                HandleDigitButton(digitIndex, isUpButton);
                return false;
            };
        }


        Buttons[8].OnInteract += delegate ()
        {
            HandleSubmit();
            return false;
        };

    }

    // Update HandleDigitButton to add interaction punch:
    void HandleDigitButton(int digitIndex, bool isUpButton) {
   if (ModuleSolved) return;
   
   // Get the specific button that was pressed
   KMSelectable pressedButton = Buttons[digitIndex * 2 + (isUpButton ? 0 : 1)];
   
   // Add interaction punch (small visual feedback)
   pressedButton.AddInteractionPunch(0.5f);
   
   // Play custom button press sound
   Audio.PlaySoundAtTransform(buttonPressSound.name, pressedButton.transform);
   
   // Increment or decrement the digit value
   if (isUpButton) {
      // Increment with wraparound from 9 to 0
      digitValues[digitIndex] = (digitValues[digitIndex] + 1) % 10;
   } else {
      // Decrement with wraparound from 0 to 9
      digitValues[digitIndex] = (digitValues[digitIndex] + 9) % 10; // Adding 9 is the same as subtracting 1 with modulo 10
   }
   
   // Update the display
   UpdateDigitDisplay(digitIndex);
}



    void UpdateDigitDisplay(int digitIndex)
    {
        if (digitIndex >= 0 && digitIndex < AnswerDigits.Length)
        {
            AnswerDigits[digitIndex].text = digitValues[digitIndex].ToString();
        }
    }

    void GenerateRandomProbabilities()
    {
        initialProbabilities = new int[6];

        for (int i = 0; i < 6; i++)
        {
            initialProbabilities[i] = Rnd.Range(1, 101); // Random int from 1-100 inclusive
        }
        for (int i = 0; i < 6; i++)
        {
            if (i < DisplayProbabilities.Length && DisplayProbabilities[i] != null)
            {
                DisplayProbabilities[i].text = initialProbabilities[i].ToString();
            }
        }
        Debug.LogFormat("[Huffman Coding #{0}] The initial probabilities are: {1}.", ModuleId, IntArrayToString(initialProbabilities));
    }

    void EdgeworkBasedProbabilities()
    {
        // Step 1: Edgework-Based Adjustments to Initial Probabilities
        Debug.LogFormat("[Huffman Coding #{0}] Step 1:", ModuleId);
        serialNumberValues = new int[6];
        adjustedProbabilities = new int[initialProbabilities.Length];
        Array.Copy(initialProbabilities, adjustedProbabilities, initialProbabilities.Length);

        // Get the serial number
        string serialNumber = Bomb.GetSerialNumber();
        Debug.LogFormat("[Huffman Coding #{0}] — Serial Number: {1}", ModuleId, serialNumber);

        // Adjustment 1: Add the value of the nth character to the nth symbol

        for (int i = 0; i < Math.Min(serialNumber.Length, adjustedProbabilities.Length); i++)
        {
            char c = serialNumber[i];
            int value;

            if (char.IsLetter(c))
            {
                // Convert letter to 1-26
                value = char.ToUpper(c) - 'A' + 1;
            }
            else
            {
                // For digits, use the numeric value
                value = c - '0';
            }
            serialNumberValues[i] = value;
            adjustedProbabilities[i] += value;
        }
        Debug.LogFormat("[Huffman Coding #{0}] — Converting the S/N to numerical values: {1}", ModuleId, IntArrayToString(serialNumberValues));
        Debug.LogFormat("[Huffman Coding #{0}] — Adding each: {1}+{2},{3}+{4},{5}+{6},{7}+{8},{9}+{10},{11}+{12}.", ModuleId, initialProbabilities[0], serialNumberValues[0], initialProbabilities[1], serialNumberValues[1], initialProbabilities[2], serialNumberValues[2], initialProbabilities[3], serialNumberValues[3], initialProbabilities[4], serialNumberValues[4], initialProbabilities[5], serialNumberValues[5]);
        Debug.LogFormat("[Huffman Coding #{0}] — Result: {1}", ModuleId, IntArrayToString(adjustedProbabilities));
        // Count letters and digits in the serial number
        int letterCount = serialNumber.Count(char.IsLetter);
        int digitCount = serialNumber.Count(char.IsDigit);
        Debug.LogFormat("[Huffman Coding #{0}] The S/N has {1} letters and {2} digits.",
            ModuleId, letterCount, digitCount);

        // Adjustment 2: Letters vs Digits
        if (letterCount > digitCount)
        {
            // More letters than digits - multiply odd-positioned symbols by 5
            Debug.LogFormat("[Huffman Coding #{0}] — No. of letters > no. of digits: Multiply odd-positioned probabilities by 5.", ModuleId);
            Debug.LogFormat("[Huffman Coding #{0}] — Multiplying each: {1}*5,{2},{3}*5,{4},{5}*5,{6}.", ModuleId, adjustedProbabilities[0], adjustedProbabilities[1], adjustedProbabilities[2],
            adjustedProbabilities[3], adjustedProbabilities[4], adjustedProbabilities[5]);
            for (int i = 0; i < adjustedProbabilities.Length; i += 2)
            {
                adjustedProbabilities[i] *= 5;
            }
            Debug.LogFormat("[Huffman Coding #{0}] — Result: {1}", ModuleId, IntArrayToString(adjustedProbabilities));
        }
        else
        {
            // Otherwise - multiply even-positioned symbols by 5
            Debug.LogFormat("[Huffman Coding #{0}] — No. of letters ≤ no. of digits: Multiply even-positioned probabilities by 5.", ModuleId);
            Debug.LogFormat("[Huffman Coding #{0}] — Multiplying each: {1},{2}*5,{3},{4}*5,{5},{6}*5.", ModuleId, adjustedProbabilities[0], adjustedProbabilities[1], adjustedProbabilities[2],
            adjustedProbabilities[3], adjustedProbabilities[4], adjustedProbabilities[5]);
            for (int i = 1; i < adjustedProbabilities.Length; i += 2)
            {
                adjustedProbabilities[i] *= 5;
            }
            Debug.LogFormat("[Huffman Coding #{0}] — Result: {1}", ModuleId, IntArrayToString(adjustedProbabilities));
        }

        // Adjustment 3: Ports vs Batteries
        int portCount = Bomb.GetPortCount();
        int batteryCount = Bomb.GetBatteryCount();
        Debug.LogFormat("[Huffman Coding #{0}] — Bomb has {1} ports and {2} batteries.",
            ModuleId, portCount, batteryCount);

        if (portCount > batteryCount)
        {
            // More ports than batteries - add 10 to even-positioned probabilities
            Debug.LogFormat("[Huffman Coding #{0}] — No. of ports > no. of batteries: Add 10 to even-positioned probabilities.", ModuleId);
            Debug.LogFormat("[Huffman Coding #{0}] — Adding each: {1},{2}+10,{3},{4}+10,{5},{6}+10.", ModuleId, adjustedProbabilities[0], adjustedProbabilities[1], adjustedProbabilities[2],
            adjustedProbabilities[3], adjustedProbabilities[4], adjustedProbabilities[5]);
            for (int i = 1; i < adjustedProbabilities.Length; i += 2)
            {
                adjustedProbabilities[i] += 10;
            }
            Debug.LogFormat("[Huffman Coding #{0}] — Result: {1}", ModuleId, IntArrayToString(adjustedProbabilities));

        }
        else
        {
            // Otherwise - add 10 to odd-positioned probabilities
            Debug.LogFormat("[Huffman Coding #{0}] — No. of ports ≤ no. of batteries: Add 10 to odd-positioned probabilities.", ModuleId);
            Debug.LogFormat("[Huffman Coding #{0}] — Adding each: {1}+10,{2},{3}+10,{4},{5}+10,{6}.", ModuleId, adjustedProbabilities[0], adjustedProbabilities[1], adjustedProbabilities[2],
            adjustedProbabilities[3], adjustedProbabilities[4], adjustedProbabilities[5]);
            for (int i = 0; i < adjustedProbabilities.Length; i += 2)
            {
                adjustedProbabilities[i] += 10;
            }
            Debug.LogFormat("[Huffman Coding #{0}] — Result: {1}", ModuleId, IntArrayToString(adjustedProbabilities));
        }

        // Adjustment 4: Check for HUFFMAN letters in serial
        bool containsHuffmanLetter = "HUFFMAN".Any(c => serialNumber.Contains(c));
        Debug.LogFormat("[Huffman Coding #{0}] — S/N {1} contain a letter from HUFFMAN.",
            ModuleId, containsHuffmanLetter ? "DOES" : "DOES NOT");
        if (containsHuffmanLetter)
        {
            // Get the highest probability
            int maxProb = adjustedProbabilities.Max();
            // Calculate its digital root
            int maxProbDigitalRoot = CalculateDigitalRootOf(maxProb);

            Debug.LogFormat("[Huffman Coding #{0}] — S/N contains a letter in HUFFMAN: Add the digital root of the highest probability ({1}, digital root: {2}) to all probabilities.",
                ModuleId, maxProb, maxProbDigitalRoot);

            // Add this digital root to all probabilities
            for (int i = 0; i < adjustedProbabilities.Length; i++)
            {
                adjustedProbabilities[i] += maxProbDigitalRoot;
            }
            Debug.LogFormat("[Huffman Coding #{0}] — Result: {1}", ModuleId, IntArrayToString(adjustedProbabilities));
        }
        else
        {
            // Get the lowest probability
            int minProb = adjustedProbabilities.Min();
            // Calculate its digital root
            int minProbDigitalRoot = CalculateDigitalRootOf(minProb);

            Debug.LogFormat("[Huffman Coding #{0}] — S/N doesn't contain a letter in HUFFMAN: Add the digital root of the lowest probability ({1}, digital root: {2}) to all probabilities.",
                ModuleId, minProb, minProbDigitalRoot);

            // Add this digital root to all probabilities
            for (int i = 0; i < adjustedProbabilities.Length; i++)
            {
                adjustedProbabilities[i] += minProbDigitalRoot;
            }
            Debug.LogFormat("[Huffman Coding #{0}] — Result: {1}", ModuleId, IntArrayToString(adjustedProbabilities));
        }

        // Fix here: Changed adjustedProbabilities.Join(", ") to string.Join(", ", adjustedProbabilities)
        Debug.LogFormat("[Huffman Coding #{0}] — Final adjusted probabilities: {1}",
            ModuleId, IntArrayToString(adjustedProbabilities));
    }

    int CalculateDigitalRootOf(int number)
    {
        // Digital root = sum of digits repeated until a single digit
        while (number >= 10)
        {
            int sum = 0;
            while (number > 0)
            {
                sum += number % 10;
                number /= 10;
            }
            number = sum;
        }
        return number;
    }

    void NormalizeProbabilities()
    {
        // Step 2: Normalize the probabilities
        Debug.LogFormat("[Huffman Coding #{0}] Step 2:", ModuleId);
        normalizedProbabilities = new int[adjustedProbabilities.Length];

        // Calculate the sum of all probabilities
        int sumProbabilities = adjustedProbabilities.Sum();
        Debug.LogFormat("[Huffman Coding #{0}] — Original sum: {1}.", ModuleId, sumProbabilities);
        Debug.LogFormat("[Huffman Coding #{0}] — Dividing each probability by {1} then rounding each to the nearest whole number:", ModuleId, sumProbabilities);
        // Normalize each probability by dividing by the sum and multiplying by 100
        for (int i = 0; i < adjustedProbabilities.Length; i++)
        {
            // Round to nearest whole number
            normalizedProbabilities[i] = Mathf.RoundToInt((float)adjustedProbabilities[i] / sumProbabilities * 100);
            Debug.LogFormat("[Huffman Coding #{0}] — Normalized probability {1}: {2} -> {3}",
                ModuleId, i + 1, adjustedProbabilities[i], normalizedProbabilities[i]);
        }

        // Check if the sum is 100%, adjust the last probability if needed
        int normalizedSum = normalizedProbabilities.Sum();
        Debug.LogFormat("[Huffman Coding #{0}] — Sum after normalization: {1}", ModuleId, normalizedSum);

        if (normalizedSum != 100)
        {
            int difference = 100 - normalizedSum;
            normalizedProbabilities[5] += difference; // Adjust the last symbol (a6)
            Debug.LogFormat("[Huffman Coding #{0}] — Adjusted probability 6 by {1} to make sum equal 100%, new value: {2}",
                ModuleId, difference, normalizedProbabilities[5]);
        }

        // Fix here: Changed normalizedProbabilities.Join(", ") to string.Join(", ", normalizedProbabilities)
        Debug.LogFormat("[Huffman Coding #{0}] — Final normalized probabilities: {1}",
            ModuleId, IntArrayToString(normalizedProbabilities));
    }

    float CalculateEntropy()
    {

        decimalProbabilities = new float[normalizedProbabilities.Length];
        float entropyValue = 0f;
        Debug.LogFormat("[Huffman Coding #{0}] Step 3: Calculating Entropy:", ModuleId);

        for (int i = 0; i < normalizedProbabilities.Length; i++)
        {
            if (normalizedProbabilities[i] > 0)
            {
                decimalProbabilities[i] = normalizedProbabilities[i] / 100f;
                float logValue = Mathf.Log(1 / decimalProbabilities[i], 2);
                float termValue = decimalProbabilities[i] * logValue;
                entropyValue += termValue;
            }
        }

        Debug.LogFormat("[Huffman Coding #{0}] — H = {1}*log_2(1/{1}) + {2}*log_2(1/{2}) + {3}*log_2(1/{3}) + {4}*log_2(1/{4}) + {5}*log_2(1/{5}) + {6}*log_2(1/{6})",
                 ModuleId, decimalProbabilities[0], decimalProbabilities[1], decimalProbabilities[2], decimalProbabilities[3], decimalProbabilities[4], decimalProbabilities[5]);

        // Round to 3 decimal places
        entropyValue = Mathf.Round(entropyValue * 1000f) / 1000f;

        Debug.LogFormat("[Huffman Coding #{0}] — Final entropy (H) to three decimal places: {1} bits/symbol",
           ModuleId, entropyValue);
        return entropyValue;
    }

    void HuffmanCodingAlgorithm()
    {
        Debug.LogFormat("[Huffman Coding #{0}] Step 4: Huffman Coding Algorithm", ModuleId);

        // Create arrays to store symbol indices and probabilities for each column
        List<List<int>> columns = new List<List<int>>();
        List<List<int>> columnProbabilities = new List<List<int>>();
        List<List<string>> columnCodes = new List<List<string>>();

        // Column 0 (initial): Sort symbols by probability descending
        var sortedIndices = Enumerable.Range(0, normalizedProbabilities.Length)
            .OrderByDescending(i => normalizedProbabilities[i])
            .ToList();

        columns.Add(new List<int>(sortedIndices));
        columnProbabilities.Add(new List<int>());
        columnCodes.Add(new List<string>());

        // Fill initial column probabilities and codes
        for (int i = 0; i < sortedIndices.Count; i++)
        {
            columnProbabilities[0].Add(normalizedProbabilities[sortedIndices[i]]);
            columnCodes[0].Add(""); // Empty codes initially
        }

        // Log initial column
        Debug.LogFormat("[Huffman Coding #{0}] — Column 0 (Initial, sorted descending):", ModuleId);
        for (int i = 0; i < columns[0].Count; i++)
        {
            Debug.LogFormat("[Huffman Coding #{0}] —   a{1}: {2}%",
                ModuleId, columns[0][i] + 1, columnProbabilities[0][i]);
        }

        int columnIndex = 1;

        // Continue until only 2 probabilities remain
        while (columnProbabilities[columnIndex - 1].Count > 2)
        {
            // Create new column
            columns.Add(new List<int>());
            columnProbabilities.Add(new List<int>());
            columnCodes.Add(new List<string>());

            var prevColumn = columnProbabilities[columnIndex - 1];
            var prevSymbols = columns[columnIndex - 1];

            // Find the two lowest probabilities (they will be at the end since we sort descending)
            int lastIndex = prevColumn.Count - 1;
            int secondLastIndex = prevColumn.Count - 2;

            int lowerProb = prevColumn[lastIndex];
            int upperProb = prevColumn[secondLastIndex];
            int combinedProb = lowerProb + upperProb;

            Debug.LogFormat("[Huffman Coding #{0}] — Column {1}: Combining lowest probabilities {2}% + {3}% = {4}%",
                ModuleId, columnIndex, upperProb, lowerProb, combinedProb);

            // Copy all probabilities except the two being combined
            for (int i = 0; i < prevColumn.Count - 2; i++)
            {
                columnProbabilities[columnIndex].Add(prevColumn[i]);
                columns[columnIndex].Add(prevSymbols[i]);
                columnCodes[columnIndex].Add("");
            }

            // Add the combined probability
            columnProbabilities[columnIndex].Add(combinedProb);
            columns[columnIndex].Add(-1); // Special marker for combined probability
            columnCodes[columnIndex].Add("");

            // Sort the new column descending
            var sortedPairs = columnProbabilities[columnIndex]
                .Select((prob, idx) => new { Probability = prob, OriginalIndex = idx, Symbol = columns[columnIndex][idx] })
                .OrderByDescending(x => x.Probability)
                .ToList();

            // Update the column with sorted values
            for (int i = 0; i < sortedPairs.Count; i++)
            {
                columnProbabilities[columnIndex][i] = sortedPairs[i].Probability;
                columns[columnIndex][i] = sortedPairs[i].Symbol;
            }

            // Log current column
            Debug.LogFormat("[Huffman Coding #{0}] — Column {1} (after sorting):", ModuleId, columnIndex);
            for (int i = 0; i < columns[columnIndex].Count; i++)
            {
                if (columns[columnIndex][i] == -1)
                {
                    Debug.LogFormat("[Huffman Coding #{0}] —   Combined: {1}%",
                        ModuleId, columnProbabilities[columnIndex][i]);
                }
                else
                {
                    Debug.LogFormat("[Huffman Coding #{0}] —   a{1}: {2}%",
                        ModuleId, columns[columnIndex][i] + 1, columnProbabilities[columnIndex][i]);
                }
            }

            columnIndex++;
        }

        // Final column should have exactly 2 probabilities
        Debug.LogFormat("[Huffman Coding #{0}] — Final Column {1}:", ModuleId, columnIndex - 1);
        for (int i = 0; i < 2; i++)
        {
            if (columns[columnIndex - 1][i] == -1)
            {
                Debug.LogFormat("[Huffman Coding #{0}] —   Combined: {1}%",
                    ModuleId, columnProbabilities[columnIndex - 1][i]);
            }
            else
            {
                Debug.LogFormat("[Huffman Coding #{0}] —   a{1}: {2}%",
                    ModuleId, columns[columnIndex - 1][i] + 1, columnProbabilities[columnIndex - 1][i]);
            }
        }

        // Assign binary codes working backwards
        Debug.LogFormat("[Huffman Coding #{0}] — Assigning binary codes:", ModuleId);

        // Start from the rightmost column and assign 0 to upper, 1 to lower
        int finalColumnIndex = columnIndex - 1;
        columnCodes[finalColumnIndex][0] = "0"; // Upper branch
        columnCodes[finalColumnIndex][1] = "1"; // Lower branch

        Debug.LogFormat("[Huffman Coding #{0}] — Column {1}: Upper = 0, Lower = 1",
            ModuleId, finalColumnIndex);

        // Work backwards through columns
        for (int col = finalColumnIndex - 1; col >= 0; col--)
        {
            Debug.LogFormat("[Huffman Coding #{0}] — Processing Column {1}:", ModuleId, col);

            // For each probability in current column
            for (int i = 0; i < columnProbabilities[col].Count; i++)
            {
                // Check if this probability was combined to form a probability in the next column
                bool wasCombined = false;

                // The two lowest in current column were combined
                if (i >= columnProbabilities[col].Count - 2)
                {
                    // This was one of the combined probabilities
                    // Find the combined probability in next column
                    int combinedProb = columnProbabilities[col][columnProbabilities[col].Count - 2] +
                                      columnProbabilities[col][columnProbabilities[col].Count - 1];

                    // Find this combined probability in next column
                    for (int j = 0; j < columnProbabilities[col + 1].Count; j++)
                    {
                        if (columnProbabilities[col + 1][j] == combinedProb && columns[col + 1][j] == -1)
                        {
                            // Assign codes based on position in combination
                            if (i == columnProbabilities[col].Count - 2)
                            {
                                // Upper of the two combined (second to last)
                                columnCodes[col][i] = columnCodes[col + 1][j] + "0";
                            }
                            else
                            {
                                // Lower of the two combined (last)
                                columnCodes[col][i] = columnCodes[col + 1][j] + "1";
                            }
                            wasCombined = true;
                            break;
                        }
                    }
                }

                if (!wasCombined)
                {
                    // This probability wasn't combined, so copy code from same position in next column
                    // Find this probability in the next column
                    for (int j = 0; j < columnProbabilities[col + 1].Count; j++)
                    {
                        if (columnProbabilities[col + 1][j] == columnProbabilities[col][i] &&
                            columns[col + 1][j] == columns[col][i])
                        {
                            columnCodes[col][i] = columnCodes[col + 1][j];
                            break;
                        }
                    }
                }

                // Log the code assignment
                if (columns[col][i] == -1)
                {
                    Debug.LogFormat("[Huffman Coding #{0}] —   Combined ({1}%): {2}",
                        ModuleId, columnProbabilities[col][i], columnCodes[col][i]);
                }
                else
                {
                    Debug.LogFormat("[Huffman Coding #{0}] —   a{1} ({2}%): {3}",
                        ModuleId, columns[col][i] + 1, columnProbabilities[col][i], columnCodes[col][i]);
                }
            }
        }

        // Log final binary codes for each symbol
        // Initialize huffmanCodes array
        huffmanCodes = new string[6];

        // Log final binary codes for each symbol and store them
        Debug.LogFormat("[Huffman Coding #{0}] — Final Huffman Codes:", ModuleId);

        for (int i = 0; i < columns[0].Count; i++)
        {
            if (columns[0][i] != -1)
            {
                int symbolIndex = columns[0][i];
                huffmanCodes[symbolIndex] = columnCodes[0][i];
                Debug.LogFormat("[Huffman Coding #{0}] —   a{1}: {2} (length: {3})",
                    ModuleId, symbolIndex + 1, huffmanCodes[symbolIndex], huffmanCodes[symbolIndex].Length);
            }
        }
    }

    // Step 5: Calculate Average Codeword Length
    float CalculateAverageCodewordLength()
    {
        Debug.LogFormat("[Huffman Coding #{0}] Step 5: Calculating Average Codeword Length (L)", ModuleId);

        float averageLength = 0f;

        for (int i = 0; i < normalizedProbabilities.Length; i++)
        {
            if (huffmanCodes[i] != null && huffmanCodes[i].Length > 0)
            {
                float probability = normalizedProbabilities[i] / 100f;
                int codeLength = huffmanCodes[i].Length;
                float contribution = probability * codeLength;
                averageLength += contribution;

            }
        }
        Debug.LogFormat("[Huffman Coding #{0}] — L = {1}*{2} + {3}*{4} + {5}*{6} + {7}*{8} + {9}*{10} + {11}*{12}",
            ModuleId,
            normalizedProbabilities[0] / 100f, huffmanCodes[0] != null ? huffmanCodes[0].Length : 0,
            normalizedProbabilities[1] / 100f, huffmanCodes[1] != null ? huffmanCodes[1].Length : 0,
            normalizedProbabilities[2] / 100f, huffmanCodes[2] != null ? huffmanCodes[2].Length : 0,
            normalizedProbabilities[3] / 100f, huffmanCodes[3] != null ? huffmanCodes[3].Length : 0,
            normalizedProbabilities[4] / 100f, huffmanCodes[4] != null ? huffmanCodes[4].Length : 0,
            normalizedProbabilities[5] / 100f, huffmanCodes[5] != null ? huffmanCodes[5].Length : 0);

        // Round to 3 decimal places
        averageLength = Mathf.Round(averageLength * 1000f) / 1000f;

        Debug.LogFormat("[Huffman Coding #{0}] — Average Codeword Length (L): {1} bits",
            ModuleId, averageLength);

        return averageLength;
    }

    // Step 6: Calculate Coding Efficiency
    float CalculateCodingEfficiency()
    {
        Debug.LogFormat("[Huffman Coding #{0}] Step 6: Calculating Coding Efficiency", ModuleId);

        float efficiency = (H / L) * 100f;

        Debug.LogFormat("[Huffman Coding #{0}] — Coding Efficiency = (H / L) * 100% = ({1} / {2}) * 100%",
            ModuleId, H, L);

        // Round to 2 decimal places
        efficiency = Mathf.Round(efficiency * 100f) / 100f;

        Debug.LogFormat("[Huffman Coding #{0}] — Coding Efficiency: {1}%",
            ModuleId, efficiency);

        return efficiency;
    }

    // Step 7: Additional Rules
    void AdditionalRules()
    {
        Debug.LogFormat("[Huffman Coding #{0}] Step 7: Additional Rules", ModuleId);

        // Remove decimal point and calculate digital root
        string efficiencyString = codingEfficiency.ToString("F2").Replace(".", "");
        int efficiencyNumber = int.Parse(efficiencyString);
        int digitalRoot = CalculateDigitalRootOf(efficiencyNumber);

        Debug.LogFormat("[Huffman Coding #{0}] — Coding efficiency without decimal: {1}",
            ModuleId, efficiencyNumber);
        Debug.LogFormat("[Huffman Coding #{0}] — Digital root: {1}",
            ModuleId, digitalRoot);

        // Store the correct answer (coding efficiency as integer, e.g., 87.65% becomes 8765)
        correctAnswer = efficiencyNumber;

        // Check number of strikes
        int strikeCount = Bomb.GetStrikes();

        if (strikeCount % 2 == 0)
        {
            // Even strikes - submit when last digit of timer matches digital root
            canSubmitAnytime = false;
            Debug.LogFormat("[Huffman Coding #{0}] — Even number of strikes ({1}): Submit when last digit of timer is {2}",
                ModuleId, strikeCount, digitalRoot);
        }
        else
        {
            // Odd strikes - submit at any time
            canSubmitAnytime = true;
            Debug.LogFormat("[Huffman Coding #{0}] — Odd number of strikes ({1}): Submit at any time",
                ModuleId, strikeCount);
        }

        Debug.LogFormat("[Huffman Coding #{0}] — Correct answer to input: {1}",
            ModuleId, correctAnswer);
    }

    // Update HandleSubmit to add interaction punch:
    void HandleSubmit() {
   if (ModuleSolved) return;
   
   // Add interaction punch for submit button
   Buttons[8].AddInteractionPunch();
   
   // Play button press sound
   Audio.PlaySoundAtTransform(buttonPressSound.name, Buttons[8].transform);
   
   // Get the current input as a number
   int inputAnswer = 0;
   for (int i = 0; i < digitValues.Length; i++) {
       inputAnswer = inputAnswer * 10 + digitValues[i];
   }
   
   Debug.LogFormat("[Huffman Coding #{0}] — Submit pressed: Input = {1}, Correct = {2}", 
       ModuleId, inputAnswer, correctAnswer);
   
   // Check if submission is allowed based on timing rules
   if (!canSubmitAnytime) {
       // Check current number of strikes (dynamically)
       int currentStrikes = Bomb.GetStrikes();
       
       if (currentStrikes % 2 == 0) {
           // Even strikes - need to check timer
           string timerText = Bomb.GetFormattedTime();
           char lastDigit = timerText[timerText.Length - 1];
           int timerLastDigit = int.Parse(lastDigit.ToString());
           
           // Get the digital root from the coding efficiency
           string efficiencyString = codingEfficiency.ToString("F2").Replace(".", "");
           int efficiencyNumber = int.Parse(efficiencyString);
           int digitalRoot = CalculateDigitalRootOf(efficiencyNumber);
           
           if (timerLastDigit != digitalRoot) {
               Debug.LogFormat("[Huffman Coding #{0}] — Submit at wrong time: Timer ends in {1}, need {2} (strikes: {3})", 
                   ModuleId, timerLastDigit, digitalRoot, currentStrikes);
               
               // Play custom strike sound
               Audio.PlaySoundAtTransform(strikeSound.name, transform);
               
               Strike();
               return;
           }
           
           Debug.LogFormat("[Huffman Coding #{0}] — Submit at correct time: Timer ends in {1} (strikes: {2})", 
               ModuleId, timerLastDigit, currentStrikes);
       } else {
           // Odd strikes - can submit anytime
           Debug.LogFormat("[Huffman Coding #{0}] — Odd strikes ({1}): Can submit anytime", 
               ModuleId, currentStrikes);
       }
   }
   
   // Check if answer is correct
   if (inputAnswer == correctAnswer) {
       Debug.LogFormat("[Huffman Coding #{0}] — Correct answer submitted!", ModuleId);
       ModuleSolved = true;
       
       // Play custom solve sound
       Audio.PlaySoundAtTransform(solveSound.name, transform);
       
       Solve();
   } else {
       Debug.LogFormat("[Huffman Coding #{0}] — Wrong answer: {1} ≠ {2}", 
           ModuleId, inputAnswer, correctAnswer);
       
       // Play custom strike sound
       Audio.PlaySoundAtTransform(strikeSound.name, transform);
       
       Strike();
   }
}



    // Update TwitchHelpMessage:
// Update TwitchHelpMessage:
#pragma warning disable 414
private readonly string TwitchHelpMessage = @"!{0} submit XX.XX% [Submits the coding efficiency percentage] | !{0} submit XX.XX% on X [Submits when timer's last digit is X] | Examples: '!{0} submit 87.65%' or '!{0} submit 87.65% on 3'";
#pragma warning restore 414

// Implement Twitch Plays support:
IEnumerator ProcessTwitchCommand(string command) {
   command = command.Trim().ToLowerInvariant();
   
   if (command.StartsWith("submit ")) {
       string[] parts = command.Split(' ');
       
       if (parts.Length < 2) {
           yield return "sendtochaterror Invalid command format. Use: submit XX.XX%";
           yield break;
       }
       
       // Check if there's an "on X" timing requirement
       bool hasTimingRequirement = false;
       int requiredLastDigit = -1;
       
       if (parts.Length >= 4 && parts[parts.Length - 2] == "on") {
           int tempDigit;
           if (int.TryParse(parts[parts.Length - 1], out tempDigit)) {
               requiredLastDigit = tempDigit;
               hasTimingRequirement = true;
           }
       }
       
       // Parse the percentage
       string percentageStr = parts[1];
       if (!percentageStr.EndsWith("%")) {
           yield return "sendtochaterror Percentage must end with %";
           yield break;
       }
       
       percentageStr = percentageStr.Substring(0, percentageStr.Length - 1);
       
       float percentage;
       if (!float.TryParse(percentageStr, out percentage)) {
           yield return "sendtochaterror Invalid percentage format. Use XX.XX format";
           yield break;
       }
       
       // Convert to integer format (remove decimal point)
       int targetValue = Mathf.RoundToInt(percentage * 100);
       
       // Set the digits
       yield return null;
       yield return SetDigitsToValue(targetValue);
       
       // Wait for correct timing if required
       if (hasTimingRequirement) {
           yield return new WaitUntil(() => {
               string timerText = Bomb.GetFormattedTime();
               char lastDigit = timerText[timerText.Length - 1];
               return int.Parse(lastDigit.ToString()) == requiredLastDigit;
           });
       }
       
       // Submit
       yield return null;
       Buttons[8].OnInteract();
   } else {
       yield return "sendtochaterror Valid commands: submit XX.XX% [on X]";
   }
}

// Helper method to set digits to a specific value
IEnumerator SetDigitsToValue(int value) {
   // Ensure value fits in 4 digits
   value = Mathf.Clamp(value, 0, 9999);
   
   // Convert to 4-digit string with leading zeros
   string valueStr = value.ToString("D4");
   
   for (int digitIndex = 0; digitIndex < 4; digitIndex++) {
       int targetDigit = int.Parse(valueStr[digitIndex].ToString());
       
       while (digitValues[digitIndex] != targetDigit) {
           // Determine shortest path (up or down)
           int currentDigit = digitValues[digitIndex];
           int distanceUp = (targetDigit - currentDigit + 10) % 10;
           int distanceDown = (currentDigit - targetDigit + 10) % 10;
           
           if (distanceUp <= distanceDown) {
               // Go up
               Buttons[digitIndex * 2].OnInteract(); // Up button
           } else {
               // Go down
               Buttons[digitIndex * 2 + 1].OnInteract(); // Down button
           }
           
           yield return new WaitForSeconds(0.1f);
       }
   }
}

IEnumerator TwitchHandleForcedSolve() {
   // Auto-solve by setting correct answer and submitting at right time
   yield return SetDigitsToValue(correctAnswer);
   
   // Wait for correct timing if needed (check strikes dynamically)
   int currentStrikes = Bomb.GetStrikes();
   if (currentStrikes % 2 == 0) {
       // Even strikes - wait for correct timer digit
       string efficiencyString = codingEfficiency.ToString("F2").Replace(".", "");
       int efficiencyNumber = int.Parse(efficiencyString);
       int digitalRoot = CalculateDigitalRootOf(efficiencyNumber);
       
       yield return new WaitUntil(() => {
           string timerText = Bomb.GetFormattedTime();
           char lastDigit = timerText[timerText.Length - 1];
           return int.Parse(lastDigit.ToString()) == digitalRoot;
       });
   }
   
   // Submit the answer
   Buttons[8].OnInteract();
}
}