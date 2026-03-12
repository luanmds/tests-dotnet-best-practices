#!/usr/bin/env bash
# =============================================================================
# run-tests.sh
# =============================================================================
# Runs integration tests with optional code coverage collection and reporting.
#
# Usage:
#   ./run-tests.sh                          # Run all tests
#   ./run-tests.sh --coverage               # Run with coverage collection
#   ./run-tests.sh --coverage --report      # Run with coverage + HTML report
#   ./run-tests.sh --project <path>         # Run specific test project
#
# Prerequisites:
#   - Docker running (for Testcontainers)
#   - .NET 9.0+ SDK
#   - For HTML reports: dotnet tool install -g dotnet-reportgenerator-globaltool
# =============================================================================

set -euo pipefail

# --- Defaults ----------------------------------------------------------------
COVERAGE=false
REPORT=false
PROJECT=""
VERBOSITY="normal"

# --- Parse arguments ---------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --coverage)
            COVERAGE=true
            shift
            ;;
        --report)
            REPORT=true
            COVERAGE=true  # Report implies coverage
            shift
            ;;
        --project)
            PROJECT="$2"
            shift 2
            ;;
        --verbose)
            VERBOSITY="detailed"
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [--coverage] [--report] [--project <path>] [--verbose]"
            echo ""
            echo "Options:"
            echo "  --coverage    Collect code coverage with coverlet"
            echo "  --report      Generate HTML coverage report (requires reportgenerator)"
            echo "  --project     Path to specific test project (default: all)"
            echo "  --verbose     Detailed test output"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# --- Pre-flight checks -------------------------------------------------------
echo "=== Pre-flight Checks ==="

# Check Docker
if ! docker info &>/dev/null; then
    echo "✗ Docker is not running. Testcontainers requires Docker."
    echo "  Start Docker and try again."
    exit 1
fi
echo "✓ Docker is running"

# Check .NET SDK
if ! command -v dotnet &>/dev/null; then
    echo "✗ .NET SDK not found."
    exit 1
fi
DOTNET_VERSION=$(dotnet --version)
echo "✓ .NET SDK $DOTNET_VERSION"

# Check reportgenerator if report requested
if [ "$REPORT" = true ]; then
    if ! command -v reportgenerator &>/dev/null; then
        echo "⚠  reportgenerator not found. Installing..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    echo "✓ reportgenerator available"
fi

echo ""

# --- Build test command -------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

CMD="dotnet test"

if [ -n "$PROJECT" ]; then
    CMD="$CMD $PROJECT"
else
    CMD="$CMD $REPO_ROOT"
fi

CMD="$CMD --logger \"console;verbosity=$VERBOSITY\""

if [ "$COVERAGE" = true ]; then
    RESULTS_DIR="$REPO_ROOT/TestResults"
    CMD="$CMD --collect:\"XPlat Code Coverage\" --results-directory \"$RESULTS_DIR\""
fi

# --- Run tests ---------------------------------------------------------------
echo "=== Running Integration Tests ==="
echo "Command: $CMD"
echo ""

eval "$CMD"
TEST_EXIT_CODE=$?

# --- Generate report ---------------------------------------------------------
if [ "$REPORT" = true ] && [ "$TEST_EXIT_CODE" -eq 0 ]; then
    echo ""
    echo "=== Generating Coverage Report ==="

    COVERAGE_FILES=$(find "$RESULTS_DIR" -name "coverage.cobertura.xml" -type f)

    if [ -z "$COVERAGE_FILES" ]; then
        echo "⚠  No coverage files found in $RESULTS_DIR"
    else
        REPORT_DIR="$REPO_ROOT/TestResults/CoverageReport"

        reportgenerator \
            -reports:"$RESULTS_DIR/**/coverage.cobertura.xml" \
            -targetdir:"$REPORT_DIR" \
            -reporttypes:Html

        echo ""
        echo "✓ Coverage report generated: $REPORT_DIR/index.html"
    fi
fi

# --- Summary -----------------------------------------------------------------
echo ""
if [ "$TEST_EXIT_CODE" -eq 0 ]; then
    echo "=== All Tests Passed ==="
else
    echo "=== Tests Failed (exit code: $TEST_EXIT_CODE) ==="
fi

exit "$TEST_EXIT_CODE"
