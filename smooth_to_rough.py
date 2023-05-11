"""
Purpose: Convert PBR Smoothness maps to Roughness.
Smoothness maps are commonly used in Unity Engine.
"""

import sys
from PIL import Image, ImageOps
from pathlib import Path

# Get the path to the image file
if len(sys.argv) > 1:
    smooth_path = Path(sys.argv[1])
else:
    print(f'Usage: python {Path(__file__).name} <image_file>')
    input()
    sys.exit()

# Open the image
try:
    with Image.open(smooth_path) as smooth:
        rough_path = smooth_path.with_stem(
            smooth_path.stem.lower().removesuffix("smoothness")
            .removesuffix("smooth")
            .removesuffix("_")
            + "_rough"
        )
        # Convert the image from smoothness to roughness
        rough = ImageOps.invert(smooth.convert("L"))
        rough.save(rough_path)
        print(f'{smooth_path} converted to {rough_path}')

except IOError as e:
    print(f'Error: Could not open {smooth_path}, {e}')
    input()
