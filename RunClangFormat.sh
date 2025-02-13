#!/usr/bin/env bash
set -e
solution_dir=$(cd "$(dirname "$0")"; pwd)
FOLDERS="src tests"
ALLOWED_EXTENSIONS_RE=".*\.\(cpp\|hpp\|c\|h\|inl\)"
EXCLUSION_RE="\(Bindings\|CLI\)/\|\(CppParser/\(Parse\)?\(Expr\|Stmt\)\)"
CLANG_FORMAT_OPTIONS="--fallback-style=none -i --verbose"

clang_format=""
VS_INSTALL_DIR=""


format()
{
    find_clang_format
    
    DRY_RUN=0
    # read -p "Dry run? (1/0): " DRY_RUN

    if [ "$DRY_RUN" -eq 1 ]; then
        CLANG_FORMAT_OPTIONS="$CLANG_FORMAT_OPTIONS --dry-run --Werror"
    fi

    # Format all files, in the specified list of directories
    # Loop through each file in the directory (and subdirectories)
    for p in $FOLDERS; do
        echo "Formatting files in folder \"$p\"..."
        cd "$solution_dir/$p"

        file_list=$(find . -type f -regex $ALLOWED_EXTENSIONS_RE -and -not -regex ".*/\($EXCLUSION_RE\).*" -print)
        
        "$clang_format" $CLANG_FORMAT_OPTIONS $file_list

        cd ..
    done
}

find_vswhere()
{
    echo "Looking for 'vswhere'..."
    VSWHERE_PATH="$(printenv 'ProgramFiles(x86)')/Microsoft Visual Studio/Installer/vswhere.exe"
    
    if [ -x "$(command -v vswhere.exe)" ]; then
        vswhere="vswhere.exe"
    elif [ -f "$VSWHERE_PATH" ]; then
        vswhere=$VSWHERE_PATH
    else
        echo -e "[91mCould not locate vswhere.exe at:\n  " $VSWHERE_PATH
        read -n 1 -s -r -p "Press any key to continue, or Ctrl+C to exit" key
        exit 1
    fi

    echo -e "Found 'vswhere.exe':\n  [90m@" $vswhere "[0m\n"
}

find_visual_studio()
{
    find_vswhere

    echo "Looking for visual studio..."

    # Find visual studio installation path
    VS_INSTALL_DIR=$("$vswhere" -latest -property installationPath)

    # Find visual studio installation path
    if [ ! -d "$VS_INSTALL_DIR" ]; then
        echo -e "[91mVisual Studio Installation directory not found at vswhere specified path:\n  " $VS_INSTALL_DIR
        read -n 1 -s -r -p "Press any key to continue, or Ctrl+C to exit" key
        exit 1
    fi

    echo -e "Found Visual Studio:\n  [90m@" $VS_INSTALL_DIR "[0m\n"
}

find_clang_format()
{
    echo "Looking for clang-format..."
    find_visual_studio

    CLANG_FORMAT_PATH="VC/Tools/Llvm/bin/clang-format.exe"
    clang_format="$VS_INSTALL_DIR/$CLANG_FORMAT_PATH"

    # Verify clang-format actually exists as well
    if ! [ -f "$clang_format" ]; then
        echo "[91mclang-format.exe could not be located at: $clang_format"
        read -n 1 -s -r -p "Press any key to continue, or Ctrl+C to exit" key
        exit 1
    fi
    
    echo -e "Found clang-format.exe:\n" "  [90mUsing" $("$clang_format" --version) "[0m\n" "  [90m@" $clang_format "[0m\n"
}

format

echo "Done!"
read -n 1 -s -r -p "Press any key to continue, or Ctrl+C to exit" key
exit 0