import path from "node:path"
import { defineConfig } from "vite"

const thisFolder = path.resolve(__dirname)
const srcFolder = path.join(thisFolder, "src")
const apiFolder = path.join(srcFolder, "api")

export default defineConfig({
  build: {
    outDir: "../server/wwwroot",
    emptyOutDir: true,
    sourcemap: true,
    lib: {
      name: "mylib",
      formats: ["es"],
      entry: ["src/main.ts"],
      fileName: (format, entryName) => `${entryName}.js`,
      cssFileName: "site",
    },
  },
  resolve: {
    alias: {
      "@db/client": srcFolder,
      "@db/api": apiFolder
    }
  },
})