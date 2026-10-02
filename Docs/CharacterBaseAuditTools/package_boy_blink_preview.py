"""Package actual Boy Unity frames with the shared technical image-layout utility."""
from pathlib import Path
import sys
sys.dont_write_bytecode=True
import package_female_blink_preview as packaging

if __name__=='__main__':
    packaging.ROOT=Path('TestResults/BoyFoundation/UnityBlink')
    packaging.main()
