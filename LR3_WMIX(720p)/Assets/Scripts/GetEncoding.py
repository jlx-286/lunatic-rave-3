r"""E:\Programs\BMS\BOFoon\Tulip\新しいテキスト ドキュメント.bms"""
r"""E:\Programs\BMS\BOF(2)\官恩俊霸 何殴秦\官恩俊霸 何殴秦\_ E STYLE.bms"""
import sys;
bms_file_path = sys.argv[1].strip("\"\'\\/.");
# bms_file_path = r"E:\Programs\BMS\BOFoon\Tulip\新しいテキスト ドキュメント.bms";
from chardet.universaldetector import UniversalDetector;
f = open(bms_file_path,"rb");
dt = UniversalDetector();
for line in f.readlines():
    try:
        dt.feed(line);
    except BaseException as e:
        # print(e);
        pass;
    if dt.done:
        break;
dt.close();
f.close();
# print(dt.result);
encoding = "SHIFT_JIS";
if dt.result["confidence"] > 0.98:
    encoding = dt.result["encoding"];
print(encoding);
return encoding;