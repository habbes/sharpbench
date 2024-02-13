import { useState, useRef, useEffect } from 'react'
import './App.css'
import "mirrorsharp-codemirror-6-preview/mirrorsharp.css";
import mirrorsharp, { MirrorSharpInstance } from 'mirrorsharp-codemirror-6-preview';

function App() {
  const containerRef = useRef<HTMLDivElement>(null);
  const ms = useRef<MirrorSharpInstance<void>>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    if (ms.current) return;
    const initial = getLanguageAndCode();
    console.log('rendering ms');
    ms.current = mirrorsharp(
      containerRef.current, {
        serviceUrl: 'ws://localhost:5176/mirrorsharp',
        language: initial.language,
        text: initial.code,
        serverOptions: (initial.mode !== 'regular' ? { 'x-mode': initial.mode } : {})
      });
    
  },);

  function handleRun() {
    if (!ms.current) return;

    const text = ms.current.getText();
    console.log('text', text);
  }

  return (
    <>
      <div>
        <button onClick={handleRun}>Run</button>
        <div ref={containerRef}></div>
      </div>
    </>
  )
}


// eslint-disable-next-line @typescript-eslint/no-explicit-any
function getCode(language: any, mode: any) {
  if (mode === 'script') {
      return 'var messages = Context.Messages;';
  }
  else if (language == 'C#') {
      return `using System;

      class C {
          const int C2 = 5;
          string f;
          string P { get; set; }
          event EventHandler e;
          event EventHandler E { add {} remove {} }

          C() {
          }

          void M(int p) {
              var l = p;
          }
      }

      class G<T> {
      }`.replace(/(\r\n|\r|\n)/g, '\r\n') // Parcel changes newlines to LF
        // eslint-disable-next-line no-regex-spaces
        .replace(/^        /gm, '');
  }
  else if (language === 'F#') {
      return '[<EntryPoint>]\r\nlet main argv = \r\n    0';
  }
  else if (language === 'IL') {
      return '.class private auto ansi \'<Module>\'\r\n{\r\n}';
  }
}

const getLanguageAndCode = () => {
  const params = window.location.hash.replace(/^\#/, '').split('&').reduce((result, item) => {
      const [key, value] = item.split('=');
      // @ts-expect-error test
      result[key] = value;
      return result;
  }, {});
  // @ts-expect-error test
  const language = (params['language'] || 'CSharp').replace('Sharp', '#');
  // @ts-expect-error test
  const mode = params['mode'] || 'regular';
  const code = getCode(language, mode);

  return { language, mode, code };
}

export default App
