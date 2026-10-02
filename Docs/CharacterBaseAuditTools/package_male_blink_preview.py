"""Package actual Male Unity captures with the shared technical layout utility.

Only the image packaging code is shared; it consumes no Female geometry or rig
data. The MP4 can be encoded separately from normal-speed/frame-%03d.png at 60fps.
"""
from pathlib import Path
import sys

sys.dont_write_bytecode=True
import package_female_blink_preview as packaging

if __name__=='__main__':
    packaging.ROOT=Path('TestResults/MaleFoundation/UnityBlink')
    packaging.main()
