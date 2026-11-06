using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartDLL;
using System.IO;

namespace TwelveCards
{
    public class PrintNow : MonoBehaviour
    {
        // Start is called before the first frame update
        SmartPrinter printer;
        public static PrintNow Instance;
        void Start()
        {
            Instance = this;
            printer = new SmartPrinter();

        }

        public void StartPrint(string[] dataset)
        {
            print("Printing " + dataset[0] + " " + dataset[1]);
            printer.PrintDocument(dataset[0], Path.Combine(Application.streamingAssetsPath, "KheloLogo.png"));
        }

        public void StartPrint(MemoryStream stream)
        {
            StreamReader reader = new StreamReader(stream);
            string allDats = reader.ReadToEnd();
            printer.PrintDocument(allDats, Path.Combine(Application.streamingAssetsPath, "KheloLogo.png"));

        }
        public void StartPrint(string path)
        {

            printer.PrintDocument(path, Path.Combine(Application.streamingAssetsPath, "KheloLogo.png"));

        }
    }
}