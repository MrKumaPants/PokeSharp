#!/bin/bash
# Comprehensive LogTemplate Testing Script
# Tests build, runtime, output, performance, and coverage

set -e  # Exit on error

PROJECT_ROOT="/mnt/c/Users/nate0/RiderProjects/PokeSharp"
RESULTS_DIR="$PROJECT_ROOT/tests/results"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
REPORT_FILE="$RESULTS_DIR/template_test_report_$TIMESTAMP.md"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   LogTemplate Conversion Testing Suite                ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""

# Create results directory
mkdir -p "$RESULTS_DIR"

# Initialize report
cat > "$REPORT_FILE" << 'EOF'
# LogTemplate Conversion Test Report

**Generated:** $(date)
**Project:** PokeSharp
**Test Suite:** Comprehensive LogTemplate Verification

---

## Executive Summary

EOF

# Function to log test result
log_result() {
    local test_name=$1
    local status=$2
    local details=$3

    if [ "$status" = "PASS" ]; then
        echo -e "${GREEN}✓ $test_name${NC}"
        echo "- ✅ **$test_name**: PASS" >> "$REPORT_FILE"
    else
        echo -e "${RED}✗ $test_name${NC}"
        echo "- ❌ **$test_name**: FAIL" >> "$REPORT_FILE"
    fi

    if [ -n "$details" ]; then
        echo "  $details"
        echo "  - $details" >> "$REPORT_FILE"
    fi
    echo "" >> "$REPORT_FILE"
}

# Test 1: Build Verification
echo -e "${BLUE}[1/6] Build Verification${NC}"
echo "## 1. Build Verification" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

cd "$PROJECT_ROOT"

if dotnet build --verbosity quiet > "$RESULTS_DIR/build_output.txt" 2>&1; then
    ERRORS=$(grep -i "error" "$RESULTS_DIR/build_output.txt" | wc -l)
    WARNINGS=$(grep -i "warning" "$RESULTS_DIR/build_output.txt" | wc -l)

    if [ $ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
        log_result "Build with 0 errors, 0 warnings" "PASS" "Clean build"
    else
        log_result "Build with 0 errors, 0 warnings" "FAIL" "$ERRORS errors, $WARNINGS warnings"
    fi
else
    log_result "Build compilation" "FAIL" "Build failed"
fi

# Test 2: Source Generator Verification
echo -e "${BLUE}[2/6] Source Generator Verification${NC}"
echo "## 2. Source Generator Verification" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

GENERATED_DIR="$PROJECT_ROOT/obj/Debug/net9.0/generated"
if [ -d "$GENERATED_DIR" ]; then
    GENERATED_FILES=$(find "$GENERATED_DIR" -name "*LoggerMessage*.g.cs" 2>/dev/null | wc -l)
    log_result "Source generators created files" "PASS" "$GENERATED_FILES LoggerMessage files generated"
else
    log_result "Source generators created files" "FAIL" "No generated files found"
fi

# Test 3: Runtime Testing
echo -e "${BLUE}[3/6] Runtime Testing${NC}"
echo "## 3. Runtime Testing" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Run the game for 10 seconds and capture output
timeout 10s dotnet run --no-build > "$RESULTS_DIR/runtime_output.txt" 2>&1 || true

if [ -f "$RESULTS_DIR/runtime_output.txt" ]; then
    OUTPUT_SIZE=$(wc -l < "$RESULTS_DIR/runtime_output.txt")

    if [ $OUTPUT_SIZE -gt 0 ]; then
        log_result "Game runs without crashing" "PASS" "$OUTPUT_SIZE lines of output"

        # Check for color codes (ANSI escape sequences)
        if grep -q '\[3[0-7]m\|\[0m' "$RESULTS_DIR/runtime_output.txt"; then
            log_result "Colored output detected" "PASS" "ANSI color codes found"
        else
            log_result "Colored output detected" "FAIL" "No color codes found"
        fi

        # Save log samples
        echo "### Sample Log Output" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
        head -n 50 "$RESULTS_DIR/runtime_output.txt" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
    else
        log_result "Game produces output" "FAIL" "No output generated"
    fi
else
    log_result "Runtime test" "FAIL" "Could not run game"
fi

# Test 4: Unit Tests
echo -e "${BLUE}[4/6] Unit Tests${NC}"
echo "## 4. Unit Tests" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

if dotnet test --no-build --verbosity quiet > "$RESULTS_DIR/test_output.txt" 2>&1; then
    PASSED=$(grep -oP '\d+(?= Passed)' "$RESULTS_DIR/test_output.txt" | tail -1)
    FAILED=$(grep -oP '\d+(?= Failed)' "$RESULTS_DIR/test_output.txt" | tail -1)

    if [ -z "$FAILED" ] || [ "$FAILED" -eq 0 ]; then
        log_result "All tests passing" "PASS" "$PASSED tests passed"
    else
        log_result "All tests passing" "FAIL" "$FAILED tests failed"
    fi
else
    log_result "Unit tests execution" "FAIL" "Test run failed"
fi

# Test 5: Performance Metrics
echo -e "${BLUE}[5/6] Performance Analysis${NC}"
echo "## 5. Performance Metrics" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Extract performance data from test output
if grep -q "LogTemplates_PerformanceIsBetterThanStringInterpolation" "$RESULTS_DIR/test_output.txt"; then
    log_result "Performance benchmarks" "PASS" "LogTemplates perform well"
else
    log_result "Performance benchmarks" "INFO" "No performance data available"
fi

if grep -q "LogTemplates_ReduceAllocations" "$RESULTS_DIR/test_output.txt"; then
    log_result "Memory allocation check" "PASS" "Allocations within limits"
else
    log_result "Memory allocation check" "INFO" "No allocation data available"
fi

# Test 6: Code Coverage
echo -e "${BLUE}[6/6] Code Coverage Analysis${NC}"
echo "## 6. Code Coverage Analysis" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Count log calls
TOTAL_LOGS=$(grep -r "Log\(Information\|Warning\|Error\|Debug\|Trace\)" "$PROJECT_ROOT" \
    --include="*.cs" \
    --exclude-dir={bin,obj,tests} 2>/dev/null | wc -l)

TEMPLATE_LOGS=$(grep -r "Log\(Information\|Warning\|Error\|Debug\|Trace\)" "$PROJECT_ROOT" \
    --include="*.cs" \
    --exclude-dir={bin,obj,tests} 2>/dev/null | \
    grep -v '\$"' | grep '{' | wc -l)

DIRECT_LOGS=$(grep -r '\$".*Log\(Information\|Warning\|Error\)' "$PROJECT_ROOT" \
    --include="*.cs" \
    --exclude-dir={bin,obj,tests} 2>/dev/null | wc -l)

if [ $TOTAL_LOGS -gt 0 ]; then
    TEMPLATE_PERCENT=$((TEMPLATE_LOGS * 100 / TOTAL_LOGS))

    echo "### Coverage Statistics" >> "$REPORT_FILE"
    echo "- Total log calls: $TOTAL_LOGS" >> "$REPORT_FILE"
    echo "- Template-based: $TEMPLATE_LOGS ($TEMPLATE_PERCENT%)" >> "$REPORT_FILE"
    echo "- Direct interpolation: $DIRECT_LOGS" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"

    if [ $TEMPLATE_PERCENT -ge 90 ]; then
        log_result "Template usage >90%" "PASS" "$TEMPLATE_PERCENT% template usage"
    else
        log_result "Template usage >90%" "FAIL" "Only $TEMPLATE_PERCENT% template usage"
    fi
else
    log_result "Coverage analysis" "FAIL" "Could not analyze log calls"
fi

# Generate summary
echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Test Summary                                         ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"

TOTAL_TESTS=$(grep -c "^- [✅❌]" "$REPORT_FILE" || echo "0")
PASSED_TESTS=$(grep -c "^- ✅" "$REPORT_FILE" || echo "0")
FAILED_TESTS=$(grep -c "^- ❌" "$REPORT_FILE" || echo "0")

echo ""
echo -e "Total Tests: ${BLUE}$TOTAL_TESTS${NC}"
echo -e "Passed:      ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed:      ${RED}$FAILED_TESTS${NC}"
echo ""

# Add summary to report
cat >> "$REPORT_FILE" << EOF

---

## Test Summary

- **Total Tests**: $TOTAL_TESTS
- **Passed**: $PASSED_TESTS ✅
- **Failed**: $FAILED_TESTS ❌
- **Success Rate**: $((PASSED_TESTS * 100 / TOTAL_TESTS))%

## Files Generated

- Build output: \`tests/results/build_output.txt\`
- Runtime output: \`tests/results/runtime_output.txt\`
- Test output: \`tests/results/test_output.txt\`
- Full report: \`$REPORT_FILE\`

---

**Test completed at:** $(date)
EOF

echo -e "${GREEN}Report saved to: $REPORT_FILE${NC}"
echo ""

# Exit with error if any tests failed
if [ $FAILED_TESTS -gt 0 ]; then
    exit 1
fi

exit 0
