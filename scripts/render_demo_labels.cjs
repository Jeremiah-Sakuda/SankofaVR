// Render code-native text labels for a silent video. No recorded scene pixels are edited here.
// PLAYWRIGHT_MODULE may point to a local installed Playwright package; no dependency downloads.
const { chromium }=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const fs=require('fs');const path=require('path');
const folder=process.argv[2];
const escape=s=>s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_EXECUTABLE||'/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',args:['--mute-audio']});
 try{
  const page=await browser.newPage({viewport:{width:960,height:66}});
  for(const file of fs.readdirSync(folder).filter(f=>f.endsWith('-label.txt'))){
   const text=fs.readFileSync(path.join(folder,file),'utf8');
   await page.setContent('<html><body style="margin:0;background:#111;color:#fff;font:18px/24px system-ui,sans-serif;padding:8px 16px;box-sizing:border-box;white-space:pre-line">'+escape(text)+'</body></html>');
   await page.screenshot({path:path.join(folder,file.replace('.txt','.png'))});
  }
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
