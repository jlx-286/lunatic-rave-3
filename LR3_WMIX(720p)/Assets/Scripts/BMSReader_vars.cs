using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
public partial class BMSReader{
    // threading
    private System.Threading.Thread thread;
    private bool inThread = true;
    private bool sorting = false;
    private bool isDone = false;
    private readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
    // other files
    private string bms_directory;
    private readonly StringBuilder file_names = new StringBuilder();
    private ushort total_medias_count = 0;
    private ushort loaded_medias_count = 0;
    private readonly string[] wav_names = Enumerable.Repeat<string>(null, 36*36).ToArray();
    private readonly string[] bmp_names = Enumerable.Repeat<string>(null, 36*36).ToArray();
    // random
    private BigInteger k = 0;
    private readonly LinkedStack<BigInteger> random_nums = new LinkedStack<BigInteger>();
    private readonly LinkedStack<ulong> ifs_count = new LinkedStack<ulong>();
    // this file
    private string[] file_lines = null;
    private string bms_file_name;
    // 
    private decimal curr_bpm;
    private bool hasScroll = false;
    private bool illegal = false;
    private Fraction32 fraction32;
    private NoteType noteType;
    private ChannelType channelType;
    private ChannelEnum channelEnum = ChannelEnum.Default, chEn = ChannelEnum.Default;
    private ushort track = 0;
    private string channel = string.Empty;
    private string message = string.Empty;
    private decimal ld = 0;
    // private double d = double.Epsilon / 2;
    private ushort u = 0;
    private byte hex_digits;
    private const long ns_per_min = TimeSpan.TicksPerMinute * 100;
    private LinkedListNode<MeasureRow> node;
    private readonly LinkedList<MeasureRow>[] measures = Enumerable.Repeat<LinkedList<MeasureRow>>(null, 1000).ToArray();
    private readonly Dictionary<ushort, decimal> exbpm_dict = new Dictionary<ushort, decimal>();
    private readonly decimal[] beats_tracks = Enumerable.Repeat(1m, 1000).ToArray();
    private readonly bool[] lnobj = Enumerable.Repeat(false, 36*36-1).ToArray();
    private readonly Dictionary<ushort, decimal> stop_dict = new Dictionary<ushort, decimal>();
    private readonly Dictionary<byte, ulong> noteCounts = new Dictionary<byte, ulong>(18);
    private readonly Dictionary<byte, NoteTimeRow[]> noteDict
        = new Dictionary<byte, NoteTimeRow[]>(18);
    private static readonly Dictionary<string, BMSChannel> channelMap
        = new Dictionary<string, BMSChannel>(){
        {"04",BMSChannel.BGA_base}, {"06",BMSChannel.BGA_poor},
        {"07",BMSChannel.BGA_layer}, {"0A",BMSChannel.BGA_layer2},
        {"03",BMSChannel.BPM3}, {"08",BMSChannel.BPM8},
        {"09",BMSChannel.Stop}, {"SC",BMSChannel.Scroll},
        // {"SP",BMSChannel.Speed},
    };
}