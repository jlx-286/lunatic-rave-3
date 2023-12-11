using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
public partial class BMSReader{
    // threading
    private Thread thread;
    private bool inThread = true;
    private bool sorting = false;
    private bool isDone = false;
    private UnityAction action;
    private ConcurrentQueue<UnityAction> unityActions = new ConcurrentQueue<UnityAction>();
    // other files
    [HideInInspector] public string bms_directory;
    private StringBuilder file_names = new StringBuilder();
    private ushort total_medias_count = 0;
    private ushort loaded_medias_count = 0;
    private readonly string[] wav_names = Enumerable.Repeat<string>(null, 36*36).ToArray();
    private readonly string[] bmp_names = Enumerable.Repeat<string>(null, 36*36).ToArray();
    // random
    private BigInteger k = 0;
    private LinkedStack<BigInteger> random_nums = new LinkedStack<BigInteger>();
    private LinkedStack<ulong> ifs_count = new LinkedStack<ulong>();
    // this file
    private string[] file_lines = null;
    [HideInInspector] public string bms_file_name;
    // 
    private decimal curr_bpm;
    private bool hasScroll = false;
    private bool illegal = false;
    private Fraction32 fraction32;
    private NoteType noteType;
    private ChannelType channelType;
    private ChannelEnum channelEnum = ChannelEnum.Default;
    private ushort track = 0;
    private string channel = string.Empty;
    private string message = string.Empty;
    private decimal ld = 0;
    // private double d = double.Epsilon / 2;
    private ushort u = 0;
    private byte hex_digits;
    private const long ns_per_min = TimeSpan.TicksPerMinute * 100;
    private long trackOffset_ns = 0;
    private long stopLen = 0;
    private int stopIndex = 0;
    private Dictionary<ushort, decimal> exbpm_dict = new Dictionary<ushort, decimal>();
    private readonly decimal[] beats_tracks = Enumerable.Repeat(1m, 1000).ToArray();
    private readonly bool[] lnobj = Enumerable.Repeat(false, 36*36-1).ToArray();
    private Dictionary<ushort, decimal> stop_dict = new Dictionary<ushort, decimal>();
    private readonly List<StopMeasureRow>[] stop_measure_list = Enumerable.Repeat<List<StopMeasureRow>>(null, 1000).ToArray();
    private readonly List<BPMMeasureRow>[] bpm_index_lists = Enumerable.Repeat<List<BPMMeasureRow>>(null, 1000).ToArray();
    private List<BPMMeasureRow> temp_bpm_index;
    private readonly byte[] laneMap = Enumerable.Repeat(byte.MaxValue, byte.MaxValue).ToArray();
    private readonly decimal[] track_end_bpms = Enumerable.Repeat(0m, 1000).ToArray();
}