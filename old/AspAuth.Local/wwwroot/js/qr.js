import qrcodegen from "./qrcodegen.js"


function create(input) {
    const qr = qrcodegen.QrCode.encodeText(input, qrcodegen.QrCode.Ecc.MEDIUM)
    const qrSvg = toSvgString(qr, 2, "#FFF", "#333")
    return qrSvg
}



function toSvgString(qr, border, lightColor, darkColor) {
      if (border < 0)
          throw new RangeError("Border must be non-negative");
      let parts = [];
      for (let y = 0; y < qr.size; y++) {
          for (let x = 0; x < qr.size; x++) {
              if (qr.getModule(x, y))
                  parts.push(`M${x + border},${y + border}h1v1h-1z`);
          }
      }
      return `
              <svg xmlns="http://www.w3.org/2000/svg" version="1.1" viewBox="0 0 ${qr.size + border * 2} ${qr.size + border * 2}" stroke="none">
              <rect width="100%" height="100%" fill="${lightColor}"/>
              <path d="${parts.join(" ")}" fill="${darkColor}"/>
              </svg>
          `;
  }
  function drawCanvas(qr, scale, border, lightColor, darkColor, canvas) {
      if (scale <= 0 || border < 0)
          throw new RangeError("Value out of range");
      const width = (qr.size + border * 2) * scale;
      canvas.width = width;
      canvas.height = width;
      let ctx = canvas.getContext("2d");
      for (let y = -border; y < qr.size + border; y++) {
          for (let x = -border; x < qr.size + border; x++) {
              ctx.fillStyle = qr.getModule(x, y) ? darkColor : lightColor;
              ctx.fillRect((x + border) * scale, (y + border) * scale, scale, scale);
          }
      }
  }


window.addEventListener("load", () => {
    const uri = document.getElementById("qrCodeData").getAttribute('data-url')
    
    var qrSvg = create(uri)

    var div = document.getElementById("qrCode")
    div.innerHTML = qrSvg
})