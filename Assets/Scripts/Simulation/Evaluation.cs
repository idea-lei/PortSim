using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public struct Data {
    public int InFieldCount;
    public int OutFieldCount;
    public int OutContainerCount;
    public int RearrangeCount;
    public TimeSpan TotalTimeSpan;
    public float avgRearrangeCount;
    public TimeSpan avgTimeSpan;

    public Data(Data data) => this = data;
}

public class Evaluation : Singleton<Evaluation> {
    [SerializeField] public Data Data;

    List<Data> records = new List<Data>();
    private readonly IFormatProvider culture = CultureInfo.CreateSpecificCulture("en-US");

    /// <summary>
    /// thsi method should be called when removing a outfield
    /// </summary>
    public void UpdateEvaluation(Container[] containers) {
        Data.OutContainerCount += containers.Length;
        foreach (var c in containers) {
            Data.RearrangeCount += c.RearrangeCount;
            Data.TotalTimeSpan += c.TotalMoveTime;
        }
        Data.avgRearrangeCount = (float)Data.RearrangeCount / Data.OutContainerCount;
        Data.avgTimeSpan = new TimeSpan(Convert.ToInt64(Data.TotalTimeSpan.Ticks * Time.timeScale / Data.OutContainerCount));
        records.Add(new Data(Data));
    }

    private void OnDestroy() {
        ToCSV();
    }

    private void ToCSV() {
        var sb = new StringBuilder("InFieldCount,OutFieldCount,OutContainerCount,RearrangeCount,avgRearrangeCount,avgTimeSpan,avgTimeSpanMinutes");
        foreach(var data in records) {
            sb.Append($"\n{data.InFieldCount},{data.OutFieldCount},{data.OutContainerCount},{data.RearrangeCount},{data.avgRearrangeCount.ToString(culture)},{data.avgTimeSpan.ToString("g", culture)}, {data.avgTimeSpan.TotalMinutes.ToString(culture)}");
        }
        string path = @"D:\works\Arbeit\UDE\TUL\Duisport\thesis\data\evaluation.csv";
        using (var writer = new StreamWriter(path, false)) {
            writer.Write(sb.ToString());
        }
    }
}
