import { defineConfig } from "vite";
import autoprefixer from "autoprefixer";

export default defineConfig({
    root: ".",
    publicDir: false,
    appType: "custom",
    build: {
        outDir: "FlexBackend.UIKit.Rcl/wwwroot/css",
        assetsDir: ".",
        cssCodeSplit: false,
        emptyOutDir: false,
        rollupOptions: {
            input: "FlexBackend.UIKit.Rcl/wwwroot/scss/site.scss",
            output: { assetFileNames: "site.css" }
        }
    },
    css: { postcss: { plugins: [autoprefixer()] } }
});
