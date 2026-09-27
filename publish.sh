#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
  echo "❌ Usage: $0 <version> (ex: $0 1.0.0)"
  exit 1
fi

VERSION="${VERSION#v}"

PROJECT_PATH="src/Host/Host.csproj"
APP_NAME="psrl"
OUTPUT_DIR="dist"
TEMP_BUILD_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "${TEMP_BUILD_DIR}"
}
trap cleanup EXIT

# Runtimes .NET : AMD64 & ARM64
declare -A TARGETS=(
  ["linux-x64"]="linux-amd64"
  ["linux-arm64"]="linux-arm64"
)

echo "🚀 Preparing self-contained archives for Passerelle v${VERSION}..."
echo "   - Version      : ${VERSION}"
echo "   - Output folder : ${OUTPUT_DIR}/"

mkdir -p "${OUTPUT_DIR}"
CHECKSUM_FILE="${OUTPUT_DIR}/checksums_v${VERSION}.txt"
rm -f "${CHECKSUM_FILE}"

for RID in "${!TARGETS[@]}"; do
  ARCH_LABEL="${TARGETS[$RID]}"
  FOLDER_NAME="${APP_NAME}-v${VERSION}-${ARCH_LABEL}"
  ARCHIVE_NAME="${FOLDER_NAME}.tar.gz"
  PUBLISH_PATH="${TEMP_BUILD_DIR}/${FOLDER_NAME}"

  echo "📦 Building and publishing for ${ARCH_LABEL} (${RID})..."
  dotnet publish "${PROJECT_PATH}" \
    --configuration Release \
    --runtime "${RID}" \
    --self-contained true \
    --output "${PUBLISH_PATH}" > /dev/null

  echo "🗜️  Creating archive ${ARCHIVE_NAME}..."
  tar -czf "${OUTPUT_DIR}/${ARCHIVE_NAME}" -C "${TEMP_BUILD_DIR}" "${FOLDER_NAME}"

  (cd "${OUTPUT_DIR}" && sha256sum "${ARCHIVE_NAME}" >> "checksums_v${VERSION}.txt")
  
  SIZE=$(du -h "${OUTPUT_DIR}/${ARCHIVE_NAME}" | cut -f1)
  echo "   ✅ Archive generated : ${OUTPUT_DIR}/${ARCHIVE_NAME} (${SIZE})"
done

echo ""
echo "🎉 All archives were successfully generated in ${OUTPUT_DIR}/!"
echo "📋 SHA-256 checksums:"
cat "${CHECKSUM_FILE}"