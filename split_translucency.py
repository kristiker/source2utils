import sys
from PIL import Image
from pathlib import Path

# Get the path to the image file
if len(sys.argv) > 1:
    image_path = Path(sys.argv[1])
else:
    print(f'Usage: python {Path(__file__)}.py <image_file>')
    input()
    sys.exit()

# Open the image
try:
    with Image.open(image_path) as img:
        new_path = image_path.with_stem(image_path.stem + "_trans").with_suffix(".png")
        img.split()[-1].save(new_path)
        print(f'{image_path} converted to {new_path}')

except IOError as e:
    print(f'Error: Could not open {image_path}, {e}')
    input()
