#!/bin/bash

# Memory Impact Measurement Script
# This script runs the game and extracts memory metrics from PerformanceMonitor logs

set -e

PROJECT_ROOT="/mnt/c/Users/nate0/RiderProjects/PokeSharp"
LOG_DIR="$PROJECT_ROOT/logs"
RESULTS_FILE="$PROJECT_ROOT/docs/memory-measurement-results.txt"

echo "========================================="
echo "Memory Impact Measurement - PokeSharp"
echo "File-Based Map Loading Refactor"
echo "========================================="
echo ""

# Create logs directory if it doesn't exist
mkdir -p "$LOG_DIR"

# Clean old logs
echo "Cleaning old logs..."
rm -f "$LOG_DIR"/*.log

# Record start time
START_TIME=$(date +%s)
echo "Starting measurement at $(date)"
echo ""

# Check database size
echo "Checking database size..."
DB_SIZE=$(find "$PROJECT_ROOT" -name "*.db" -path "*/GameData/*" -exec du -h {} \; 2>/dev/null || echo "Database not found")
echo "Database size: $DB_SIZE"
echo ""

# Build the project
echo "Building project..."
cd "$PROJECT_ROOT"
dotnet build --configuration Release > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi
echo ""

# Run the game for 60 seconds to collect metrics
echo "Running game for 60 seconds to collect metrics..."
echo "(You may see game output below - this is normal)"
echo ""

timeout 60s dotnet run --project PokeSharp.Game --configuration Release 2>&1 | tee /tmp/pokesharp-run.log || true

# Extract memory statistics from logs
echo ""
echo "========================================="
echo "Extracting Memory Statistics"
echo "========================================="
echo ""

# Find the most recent log file
LATEST_LOG=$(ls -t "$LOG_DIR"/*.log 2>/dev/null | head -1)

if [ -z "$LATEST_LOG" ]; then
    # Try console output if no log file
    LATEST_LOG="/tmp/pokesharp-run.log"
fi

if [ -f "$LATEST_LOG" ]; then
    echo "Analyzing log: $LATEST_LOG"
    echo ""

    # Extract memory statistics
    MEMORY_STATS=$(grep -i "memory statistics" "$LATEST_LOG" || echo "No memory statistics found")

    if [ "$MEMORY_STATS" != "No memory statistics found" ]; then
        echo "Memory Statistics Found:"
        echo "$MEMORY_STATS"
        echo ""

        # Extract average memory usage
        AVG_MEMORY=$(echo "$MEMORY_STATS" | grep -oP '\d+\.\d+ MB' | awk '{sum+=$1; count++} END {if (count>0) print sum/count " MB"; else print "N/A"}')

        # Extract GC counts (last reading)
        LAST_GC=$(echo "$MEMORY_STATS" | tail -1 | grep -oP 'Gen0=\d+ Gen1=\d+ Gen2=\d+' || echo "N/A")

        # Count occurrences
        STAT_COUNT=$(echo "$MEMORY_STATS" | wc -l)

        echo "Summary:"
        echo "  - Log entries: $STAT_COUNT"
        echo "  - Average memory: $AVG_MEMORY"
        echo "  - Final GC counts: $LAST_GC"
    else
        echo "⚠️ No memory statistics found in logs"
        echo "This might mean the game didn't run long enough or logging is disabled"
    fi
else
    echo "❌ No log file found"
fi

echo ""

# Save results
cat > "$RESULTS_FILE" <<EOF
Memory Impact Measurement Results
==================================
Date: $(date)
Build: Release
Duration: 60 seconds

Database Size:
$DB_SIZE

Memory Statistics:
$MEMORY_STATS

Summary:
  - Log entries: ${STAT_COUNT:-0}
  - Average memory: ${AVG_MEMORY:-N/A}
  - Final GC counts: ${LAST_GC:-N/A}

Expected Results (File-Based Implementation):
  - Memory usage: <50 MB (target)
  - GC Gen0: <10 collections/5sec
  - GC Gen2: 0 collections (no memory pressure)
  - Database size: <10 MB (metadata only)

Notes:
- If memory > 100 MB, check if map JSON is still in database
- If GC Gen2 > 0, indicates memory pressure
- Compare with historical data (before refactor: ~650 MB)
EOF

echo "Results saved to: $RESULTS_FILE"
echo ""
echo "========================================="
echo "Measurement Complete"
echo "========================================="
echo ""
echo "Next Steps:"
echo "1. Review results in $RESULTS_FILE"
echo "2. Compare with expected values"
echo "3. If memory > 100 MB, investigate database schema"
echo "4. Check PerformanceMonitor logs for detailed metrics"
