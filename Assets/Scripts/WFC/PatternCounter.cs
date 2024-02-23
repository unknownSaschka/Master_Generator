using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.WFC
{
    public class PatternCounter
    {
        public static string CountPixels(string filePath, string resultsFolderpath)
        {
            byte[] patternFileData = File.ReadAllBytes(filePath);
            Texture2D pattern = new Texture2D(2, 2);
            pattern.LoadImage(patternFileData);

            int patternSize = pattern.height;
            string[] resultFiles = Directory.GetFiles(resultsFolderpath, "*.png");
            Texture2D result = new Texture2D(2, 2);

            int amount = 0, totalMin = int.MaxValue, totalMax = int.MinValue;
            string minFile = "", maxFile = "";
            int totalPatternAmount = 0;

            string allAmount = "";

            foreach (string resultFile in resultFiles)
            {
                int localAmount = 0;
                byte[] resultFileData = File.ReadAllBytes(resultFile);
                result.LoadImage(resultFileData);
                totalPatternAmount = result.height - patternSize + 1;

                for(int y = 0; y < result.height - patternSize; y++)
                {
                    for(int x = 0; x < result.width - patternSize; x++)
                    {
                        if (isIdentical(x, y, result, pattern))
                        {
                            localAmount++;
                            amount++;
                        }
                    }
                }

                if (localAmount > totalMax) 
                { 
                    totalMax = localAmount;
                    maxFile = resultFile;
                }

                if (localAmount < totalMin)
                {
                    totalMin = localAmount;
                    minFile = resultFile;
                }

                allAmount += localAmount + "\r\n";
            }

            string statisticFile = resultsFolderpath + Path.DirectorySeparatorChar + "patternCount.txt";
            if(File.Exists(statisticFile))
            {
                File.Delete(statisticFile);
            }

            File.WriteAllText(statisticFile, allAmount);

            //return (amount / (double)resultFiles.Length , resultFiles.Length, totalPatternAmount * totalPatternAmount);
            return $"Average Pattern amount from {resultFiles.Length} Results: {amount / (double)resultFiles.Length} (min:{totalMin}, max: {totalMax}) from total {totalPatternAmount * totalPatternAmount} Patterns\r\nMinFile:{minFile}, maxFile: {maxFile}";
        }

        private static bool isIdentical(int x, int y, Texture2D result, Texture2D pattern)
        {
            int patternSize = pattern.height;

            for(int i = 0; i < patternSize; i++) 
            {
                for(int j = 0; j < patternSize; j++)
                {
                    Color cPat = pattern.GetPixel(i, j);
                    Color cRes = result.GetPixel(i + x, j + y);

                    if (cPat != cRes) return false;
                }
            }

            return true;
        }

        public static void WriteTimings(string timings, string folderPath)
        {
            string timingsFilename = "timings.txt";
            if(File.Exists(folderPath + timingsFilename))
            {
                //append
                File.AppendAllText(folderPath + timingsFilename, timings);
            }
            else
            {
                File.WriteAllText(folderPath + timingsFilename, timings);
            }
        }
    }
}
