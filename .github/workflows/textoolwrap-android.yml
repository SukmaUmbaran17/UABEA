name: Build TexToolWrap Android

on:
  workflow_dispatch:
  push:
    paths:
      - "TexToolWrap/**"
      - ".github/workflows/build-textoolwrap-android.yml"

jobs:

  build-textoolwrap:

    name: Build TexToolWrap Android ARM64

    runs-on: ubuntu-latest

    steps:

      # ============================================================
      # CHECKOUT
      # ============================================================

      - name: Checkout repository
        uses: actions/checkout@v4

        with:
          submodules: recursive
          fetch-depth: 0


      # ============================================================
      # ANDROID SDK
      # ============================================================

      - name: Setup Android SDK
        uses: android-actions/setup-android@v3


      # ============================================================
      # NDK
      # ============================================================

      - name: Install Android NDK
        shell: bash

        run: |

          echo "======================================="
          echo "INSTALL ANDROID NDK"
          echo "======================================="

          sdkmanager --install "platform-tools"

          sdkmanager --install "platforms;android-34"

          sdkmanager --install "build-tools;34.0.0"

          sdkmanager --install "ndk;26.3.11579264"


      # ============================================================
      # CONFIGURE NDK
      # ============================================================

      - name: Configure NDK
        shell: bash

        run: |

          NDK_HOME="$ANDROID_SDK_ROOT/ndk/26.3.11579264"

          if [ ! -d "$NDK_HOME" ]; then

            echo "ERROR: Android NDK tidak ditemukan"

            exit 1

          fi

          echo "NDK_HOME=$NDK_HOME" >> "$GITHUB_ENV"

          echo "$NDK_HOME" >> "$GITHUB_PATH"

          echo ""

          "$NDK_HOME/ndk-build" --version


      # ============================================================
      # VERIFY
      # ============================================================

      - name: Verify TexToolWrap
        shell: bash

        run: |

          echo "======================================="
          echo "VERIFY REQUIRED FILES"
          echo "======================================="

          FAILED=0


          check_file() {

            if [ -f "$1" ]; then

              echo "✓ FILE : $1"

            else

              echo "✗ FILE MISSING : $1"

              FAILED=1

            fi

          }


          check_dir() {

            if [ -d "$1" ]; then

              echo "✓ DIR  : $1"

            else

              echo "✗ DIR MISSING : $1"

              FAILED=1

            fi

          }


          check_file \
            "TexToolWrap/textoolwrap.cpp"


          check_file \
            "TexToolWrap/Android/CMakeLists.txt"


          check_dir \
            "TexToolWrap/PVRTexLib"


          check_dir \
            "TexToolWrap/ispc"


          check_dir \
            "TexToolWrap/crunch"


          echo ""

          echo "======================================="
          echo "TEXTOOLWRAP TREE"
          echo "======================================="

          find TexToolWrap \
            -maxdepth 4 \
            -print | sort


          if [ "$FAILED" -ne 0 ]; then

            echo ""

            echo "VERIFY FAILED"

            exit 1

          fi


      # ============================================================
      # PVRTEXLIB INSPECTION
      # ============================================================

      - name: Inspect PVRTexLib
        shell: bash

        run: |

          echo "======================================="
          echo "PVRTEXLIB INSPECTION"
          echo "======================================="

          echo ""

          echo "Directories:"

          find TexToolWrap/PVRTexLib \
            -type d \
            -print | sort


          echo ""

          echo "Files:"

          find TexToolWrap/PVRTexLib \
            -type f \
            -print | sort


          echo ""

          echo "C/C++:"

          find TexToolWrap/PVRTexLib \
            -type f \
            \( \
              -name "*.cpp" \
              -o -name "*.cc" \
              -o -name "*.cxx" \
              -o -name "*.c" \
              -o -name "*.h" \
              -o -name "*.hpp" \
            \) \
            -print | sort


          echo ""

          echo "Libraries:"

          find TexToolWrap/PVRTexLib \
            -type f \
            \( \
              -name "*.a" \
              -o -name "*.so" \
              -o -name "*.lib" \
              -o -name "*.dll" \
            \) \
            -print | sort


      # ============================================================
      # CLEAN BUILD
      # ============================================================

      - name: Clean build
        shell: bash

        run: |

          rm -rf \
            TexToolWrap/Android/build

          mkdir -p \
            TexToolWrap/Android/build


      # ============================================================
      # CMAKE
      # ============================================================

      - name: Configure CMake ARM64
        shell: bash

        run: |

          echo "======================================="
          echo "CMAKE CONFIGURE ARM64"
          echo "======================================="

          cd \
            TexToolWrap/Android/build


          cmake .. \
            -G Ninja \
            -DCMAKE_BUILD_TYPE=Release \
            -DCMAKE_TOOLCHAIN_FILE="$NDK_HOME/build/cmake/android.toolchain.cmake" \
            -DANDROID_ABI=arm64-v8a \
            -DANDROID_PLATFORM=android-24 \
            -DCMAKE_POSITION_INDEPENDENT_CODE=ON


      # ============================================================
      # BUILD
      # ============================================================

      - name: Build TexToolWrap
        shell: bash

        run: |

          echo "======================================="
          echo "BUILD TEXTOOLWRAP"
          echo "======================================="

          cd \
            TexToolWrap/Android/build


          cmake \
            --build . \
            --config Release \
            --parallel 2 \
            --verbose


      # ============================================================
      # FIND OUTPUT
      # ============================================================

      - name: Find generated libraries
        shell: bash

        run: |

          echo "======================================="
          echo "GENERATED LIBRARIES"
          echo "======================================="


          find \
            TexToolWrap/Android/build \
            -type f \
            \( \
              -name "*.so" \
              -o -name "*.a" \
            \) \
            -print | sort


      # ============================================================
      # VERIFY
      # ============================================================

      - name: Verify libtextoolwrap.so
        shell: bash

        run: |

          echo "======================================="
          echo "VERIFY TEXTOOLWRAP LIBRARY"
          echo "======================================="


          LIB="TexToolWrap/Android/build/libtextoolwrap.so"


          if [ ! -f "$LIB" ]; then

            echo ""
            echo "ERROR:"
            echo "libtextoolwrap.so tidak ditemukan."

            echo ""

            find \
              TexToolWrap/Android/build \
              -type f \
              -name "*.so" \
              -print

            exit 1

          fi


          echo ""

          echo "✓ libtextoolwrap.so BERHASIL"

          ls -lh "$LIB"


      # ============================================================
      # PREPARE APK JNI
      # ============================================================

      - name: Prepare Android JNI
        shell: bash

        run: |

          echo "======================================="
          echo "PREPARE JNI"
          echo "======================================="


          rm -rf output


          mkdir -p \
            output/arm64-v8a


          cp \
            TexToolWrap/Android/build/libtextoolwrap.so \
            output/arm64-v8a/


          echo ""

          find output \
            -type f \
            -print


      # ============================================================
      # CHECK ARCH
      # ============================================================

      - name: Check library architecture
        shell: bash

        run: |

          echo "======================================="
          echo "CHECK ELF ARCHITECTURE"
          echo "======================================="


          file \
            output/arm64-v8a/libtextoolwrap.so


      # ============================================================
      # UPLOAD
      # ============================================================

      - name: Upload TexToolWrap
        uses: actions/upload-artifact@v4

        with:

          name: TexToolWrap-Android-arm64-v8a

          path: output/

          if-no-files-found: error


      # ============================================================
      # SUMMARY
      # ============================================================

      - name: Final summary
        shell: bash

        run: |

          echo ""

          echo "======================================="
          echo "TEXTOOLWRAP ANDROID BUILD"
          echo "======================================="

          echo ""

          echo "ABI:"
          echo "arm64-v8a"

          echo ""

          echo "Output:"
          echo "output/arm64-v8a/libtextoolwrap.so"

          echo ""

          echo "Artifact:"
          echo "TexToolWrap-Android-arm64-v8a"

          echo ""

          echo "======================================="
