#!/bin/bash
# Detailed analysis of log template usage across the codebase

PROJECT_ROOT="/mnt/c/Users/nate0/RiderProjects/PokeSharp"
OUTPUT_FILE="$PROJECT_ROOT/tests/results/log_coverage_analysis.txt"

mkdir -p "$(dirname "$OUTPUT_FILE")"

echo "LogTemplate Coverage Analysis" > "$OUTPUT_FILE"
echo "=============================" >> "$OUTPUT_FILE"
echo "Generated: $(date)" >> "$OUTPUT_FILE"
echo "" >> "$OUTPUT_FILE"

# Find all C# files excluding bin/obj/tests
find "$PROJECT_ROOT" -name "*.cs" \
    -not -path "*/bin/*" \
    -not -path "*/obj/*" \
    -not -path "*/tests/*" \
    -type f | while read -r file; do

    # Check if file contains log calls
    if grep -q "Log\(Information\|Warning\|Error\|Debug\|Trace\|Critical\)" "$file"; then
        echo "File: ${file#$PROJECT_ROOT/}" >> "$OUTPUT_FILE"
        echo "----------------------------------------" >> "$OUTPUT_FILE"

        # Find template-based logs
        TEMPLATE_COUNT=$(grep -c 'Log.*".*{.*}.*"' "$file" 2>/dev/null || echo "0")

        # Find direct interpolation logs
        DIRECT_COUNT=$(grep -c 'Log.*\$"' "$file" 2>/dev/null || echo "0")

        # Find string.Format logs
        FORMAT_COUNT=$(grep -c 'Log.*string\.Format' "$file" 2>/dev/null || echo "0")

        echo "  Template-based logs: $TEMPLATE_COUNT" >> "$OUTPUT_FILE"
        echo "  Direct interpolation: $DIRECT_COUNT" >> "$OUTPUT_FILE"
        echo "  string.Format: $FORMAT_COUNT" >> "$OUTPUT_FILE"

        if [ $DIRECT_COUNT -gt 0 ] || [ $FORMAT_COUNT -gt 0 ]; then
            echo "  ⚠️  NEEDS CONVERSION:" >> "$OUTPUT_FILE"

            # Show line numbers of non-template logs
            grep -n 'Log.*\$"' "$file" 2>/dev/null | while read -r line; do
                echo "    Line ${line%%:*}: Direct interpolation" >> "$OUTPUT_FILE"
            done

            grep -n 'Log.*string\.Format' "$file" 2>/dev/null | while read -r line; do
                echo "    Line ${line%%:*}: string.Format" >> "$OUTPUT_FILE"
            done
        fi

        echo "" >> "$OUTPUT_FILE"
    fi
done

# Summary statistics
echo "" >> "$OUTPUT_FILE"
echo "Summary Statistics" >> "$OUTPUT_FILE"
echo "==================" >> "$OUTPUT_FILE"
echo "" >> "$OUTPUT_FILE"

TOTAL_FILES=$(find "$PROJECT_ROOT" -name "*.cs" \
    -not -path "*/bin/*" \
    -not -path "*/obj/*" \
    -not -path "*/tests/*" \
    -type f | wc -l)

FILES_WITH_LOGS=$(grep -l "Log\(Information\|Warning\|Error\|Debug\|Trace\)" \
    $(find "$PROJECT_ROOT" -name "*.cs" \
        -not -path "*/bin/*" \
        -not -path "*/obj/*" \
        -not -path "*/tests/*" \
        -type f) 2>/dev/null | wc -l)

TOTAL_LOG_CALLS=$(grep -r "Log\(Information\|Warning\|Error\|Debug\|Trace\)" "$PROJECT_ROOT" \
    --include="*.cs" \
    --exclude-dir={bin,obj,tests} 2>/dev/null | wc -l)

TEMPLATE_CALLS=$(grep -r 'Log.*".*{.*}.*"' "$PROJECT_ROOT" \
    --include="*.cs" \
    --exclude-dir={bin,obj,tests} 2>/dev/null | wc -l)

DIRECT_CALLS=$(grep -r 'Log.*\$"' "$PROJECT_ROOT" \
    --include="*.cs" \
    --exclude-dir={bin,obj,tests} 2>/dev/null | wc -l)

if [ $TOTAL_LOG_CALLS -gt 0 ]; then
    TEMPLATE_PERCENT=$((TEMPLATE_CALLS * 100 / TOTAL_LOG_CALLS))
else
    TEMPLATE_PERCENT=0
fi

echo "Total C# files: $TOTAL_FILES" >> "$OUTPUT_FILE"
echo "Files with logging: $FILES_WITH_LOGS" >> "$OUTPUT_FILE"
echo "" >> "$OUTPUT_FILE"
echo "Total log calls: $TOTAL_LOG_CALLS" >> "$OUTPUT_FILE"
echo "Template-based: $TEMPLATE_CALLS ($TEMPLATE_PERCENT%)" >> "$OUTPUT_FILE"
echo "Direct interpolation: $DIRECT_CALLS" >> "$OUTPUT_FILE"
echo "" >> "$OUTPUT_FILE"

if [ $TEMPLATE_PERCENT -ge 90 ]; then
    echo "✅ GOAL ACHIEVED: >90% template usage" >> "$OUTPUT_FILE"
elif [ $TEMPLATE_PERCENT -ge 75 ]; then
    echo "⚠️  GOOD PROGRESS: >75% template usage" >> "$OUTPUT_FILE"
else
    echo "❌ MORE WORK NEEDED: <75% template usage" >> "$OUTPUT_FILE"
fi

cat "$OUTPUT_FILE"
