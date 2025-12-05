@echo off
REM Batch script to process all outline sprite sheets with transparency
REM Requires Python 3 and Pillow (pip install Pillow)

echo Processing popup outline sprite sheets...
echo.

cd Outlines
python process_transparency.py .

echo.
echo Done!
pause


