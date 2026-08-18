const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const projectRoot = path.resolve(__dirname, '..', 'winui3-osd');
const sourceRoot = path.join(projectRoot, 'Assets', 'FluentSystemIcons');
const outputIco = path.join(projectRoot, 'Assets', 'osd.ico');
const previewPng = path.join(__dirname, 'modern-tray-icon-preview.png');
const sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];

function fluentSourceSize(size) {
  if (size <= 16) return 16;
  if (size <= 20) return 20;
  if (size <= 28) return 24;
  return 32;
}

function whiteGaugeSvg(sourceSize) {
  const file = path.join(sourceRoot, `ic_fluent_gauge_${sourceSize}_filled.svg`);
  return Buffer.from(fs.readFileSync(file, 'utf8').replaceAll('#212121', '#FFFFFF'));
}

async function renderIcon(size) {
  return sharp(whiteGaugeSvg(fluentSourceSize(size)))
    .resize(size, size, { fit: 'contain' })
    .png({ compressionLevel: 9, adaptiveFiltering: true })
    .toBuffer();
}

function writePngIco(frames, destination) {
  const headerSize = 6;
  const entrySize = 16;
  let offset = headerSize + entrySize * frames.length;
  const header = Buffer.alloc(headerSize);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(frames.length, 4);

  const entries = frames.map(({ size, png }) => {
    const entry = Buffer.alloc(entrySize);
    entry.writeUInt8(size === 256 ? 0 : size, 0);
    entry.writeUInt8(size === 256 ? 0 : size, 1);
    entry.writeUInt8(0, 2);
    entry.writeUInt8(0, 3);
    entry.writeUInt16LE(1, 4);
    entry.writeUInt16LE(32, 6);
    entry.writeUInt32LE(png.length, 8);
    entry.writeUInt32LE(offset, 12);
    offset += png.length;
    return entry;
  });

  fs.writeFileSync(destination, Buffer.concat([header, ...entries, ...frames.map(frame => frame.png)]));
}

async function writePreview(frames) {
  const canvasWidth = 1060;
  const canvasHeight = 360;
  const iconSizes = [16, 20, 24, 32, 48, 64, 128];
  const items = [];
  let x = 54;

  for (const size of iconSizes) {
    const frame = frames.find(item => item.size === size);
    const display = size < 48 ? size * 3 : size;
    const rendered = await sharp(frame.png).resize(display, display, { kernel: 'nearest' }).png().toBuffer();
    items.push({ input: rendered, left: x + Math.round((128 - display) / 2), top: 92 + Math.round((128 - display) / 2) });
    items.push({
      input: Buffer.from(`<svg width="128" height="34" xmlns="http://www.w3.org/2000/svg"><text x="64" y="24" text-anchor="middle" fill="#D6D9E0" font-family="Segoe UI" font-size="17">${size} px</text></svg>`),
      left: x,
      top: 226,
    });
    x += 142;
  }

  const background = Buffer.from(`
    <svg width="${canvasWidth}" height="${canvasHeight}" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="bg" x1="0" y1="0" x2="0" y2="${canvasHeight}">
          <stop offset="0" stop-color="#292B30"/>
          <stop offset="1" stop-color="#111216"/>
        </linearGradient>
      </defs>
      <rect width="${canvasWidth}" height="${canvasHeight}" rx="26" fill="url(#bg)"/>
      <text x="52" y="52" fill="#FFFFFF" font-family="Segoe UI" font-size="25" font-weight="600">Monochrome Fluent tray icon · real pixel sizes</text>
      <text x="52" y="318" fill="#B8BEC9" font-family="Segoe UI" font-size="18">Pure white Gauge glyph · transparent background · no shadow or tile</text>
    </svg>`);

  await sharp(background).composite(items).png().toFile(previewPng);
}

(async () => {
  const frames = [];
  for (const size of sizes) {
    frames.push({ size, png: await renderIcon(size) });
  }
  writePngIco(frames, outputIco);
  await writePreview(frames);
  console.log(`ICO: ${outputIco}`);
  console.log(`Preview: ${previewPng}`);
  console.log(`Frames: ${sizes.join(', ')}`);
})().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
