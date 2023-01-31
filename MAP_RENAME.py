import sys
from pathlib import Path

vpk: Path = Path(sys.argv[0]).parent / "vpk.exe"
map_vpk: Path
map_meta: Path

if not vpk.is_file():
    input("Couldn't find vpk.exe! Please add me to sbox/bin/win64")
    sys.exit(1)

if len(sys.argv) < 2:
    map_vpk = Path(input("Enter path to your existing map: "))
else:
    map_vpk = Path(sys.argv[1])

if not map_vpk.is_file():
    input(f"Couldn't find map: {map_vpk}")
    sys.exit(2)


extracted_folder: Path = map_vpk.parent / map_vpk.stem
lumps_folder: Path = extracted_folder / "maps" / map_vpk.stem
vmap_c: Path = extracted_folder / "maps" / map_vpk.with_suffix(".vmap_c").name

new_name: str = input("Please enter new map name: ").lower().split(".")[0]

import os, shutil, subprocess

if (extracted_folder.exists()):
    shutil.rmtree(extracted_folder)

subprocess.run(["vpk.exe", map_vpk.as_posix()])

new_map_folder: Path = extracted_folder.with_name(new_name)

if (new_map_folder.exists()):
    shutil.rmtree(new_map_folder)

os.rename(vmap_c, vmap_c.with_stem(new_name))
os.rename(lumps_folder, lumps_folder.with_name(new_name))
os.rename(extracted_folder, extracted_folder.with_name(new_name))

new_lumps: Path = new_map_folder / "maps" / new_name

import struct

def fix_vrman_data(data):
    nulls: list[int] = []
    last = -1
    for i, byte in enumerate(data[::-1], 1):
        if last == 0:
            if byte == 0:
                break
            nulls.append(i)
        last = byte

    i -= 2
    i += len(nulls) * 4
    i = len(data) - i

    assert struct.unpack("<I", data[i:i+4]) == (len(nulls)-1)*4

    new = b''
    for null in nulls.reverse():
        new += struct.pack("<I", null)

    return new

for vrman in new_lumps.rglob("*.vrman_c"):
    with vrman.open("rb+") as fp:
        old: bytes = fp.read()
        new: bytes = old.replace(b"maps/" + map_vpk.stem.encode("ascii"), b"maps/" + new_name.encode("ascii"))
        print(fix_vrman_data(new))
        fp.seek(0)
        # resource size
        fp.write(struct.pack("<I", len(new))  + new[4:])
        fp.truncate()
