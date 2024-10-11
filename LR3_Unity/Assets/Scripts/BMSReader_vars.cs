using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
#if UNITY_5_3_OR_NEWER
// using Unity.Collections;
// using UnityEngine;
using Debug = UnityEngine.Debug;
#endif
/*#if UNITY_2018_1_OR_NEWER
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
// using Unity.Jobs.LowLevel;
// using Unity.Jobs.LowLevel.Unsafe;
// using UnityEngine.Jobs;
// using static Unity.Jobs.IJobExtensions;
// using static Unity.Jobs.IJobParallelForExtensions;
// using static UnityEngine.Jobs.IJobParallelForTransformExtensions;
[BurstCompile]
#endif*/
public partial class BMSReader{
    // threading
// #if !UNITY_2018_1_OR_NEWER
    private System.Threading.Thread thread;
// #endif
    private bool inThread = true;
    private bool sorting = false;
    private bool isDone = false;
    private readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
    // other files
    private ushort total_medias_count = 0;
    private ushort loaded_medias_count = 0;
    // random
    private readonly LinkedStack<BigInteger> random_nums = new LinkedStack<BigInteger>();
    private readonly LinkedStack<ulong> ifs_count = new LinkedStack<ulong>();
    // this file
    private string[] file_lines = null;
    // 
    private bool hasScroll = false;
    private bool illegal = false;
    private const long ns_per_min = TimeSpan.TicksPerMinute * 100;
    private LinkedListNode<MeasureRow> node;
    private readonly LinkedList<MeasureRow>[] measures = Enumerable.Repeat<LinkedList<MeasureRow>>(null, 1000).ToArray();
    private readonly decimal[] beats_tracks = Enumerable.Repeat(1m, 1000).ToArray();
    private readonly bool[] lnobj = Enumerable.Repeat(false, 36*36-1).ToArray();
    private readonly Dictionary<ushort, decimal> stop_dict = new Dictionary<ushort, decimal>();
    private readonly Dictionary<byte, ulong> noteCounts = new Dictionary<byte, ulong>(18);
    private readonly Dictionary<byte, NoteTimeRow[]> noteDict
        = new Dictionary<byte, NoteTimeRow[]>(18);
}
/*#if UNITY_2018_1_OR_NEWER
[BurstCompile]
public class BMSReaderJobSystem : JobComponentSystem{
    private static BMSReader instance = null;
    private static bool toStart = false;
    private static JobHandle handle;
    private struct Job : IJob{
        public void Execute(){
            Stopwatch watch = new Stopwatch();
            watch.Restart();
            instance.ReadScript();
            watch.Stop();
            Debug.Log(watch.ElapsedMilliseconds);
        }
    }
    private static readonly Job job;
    public static void Start(BMSReader reader){
        instance = reader;
        toStart = true;
    }
    public static void Stop(){
        handle.Complete();
        // while(!handle.IsCompleted);
        // toStart = false;
        instance = null;
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps){
        if(toStart){
            toStart = false;
            handle = job.Schedule(inputDeps);
        }
        return inputDeps;
    }
}
#endif*/