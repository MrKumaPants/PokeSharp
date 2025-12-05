#!/bin/bash
# Shell script to process all outline sprite sheets with transparency
# Requires Python 3 and Pillow (pip install Pillow)

echo "Processing popup outline sprite sheets..."
echo ""

cd Outlines
python3 process_transparency.py .

echo ""
echo "Done!"


