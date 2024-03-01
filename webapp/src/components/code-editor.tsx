import { useRef, useEffect } from 'react'
import "mirrorsharp-codemirror-6-preview/mirrorsharp.css";
import mirrorsharp, { MirrorSharpInstance } from 'mirrorsharp-codemirror-6-preview';

export interface CodeEditorProps {
    serverUrl: string;
    onTextChange?: (text: string) => void;
    code?: string;
}

const LANGUAGE = 'C#';

export function CodeEditor({
    onTextChange,
    serverUrl,
    code
} : CodeEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const ms = useRef<MirrorSharpInstance<void>>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    if (ms.current && code) {
      ms.current.setText(code);
    }
    else if (!ms.current) {
      console.log('rendering ms');
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      ms.current = mirrorsharp(
        containerRef.current, {
          serviceUrl: serverUrl,
          language: LANGUAGE,
          text: code,
          serverOptions: { 'x-mode': 'regular' },
          on: {
              textChange: getText => onTextChange && onTextChange(getText())
          }
        });
    }
  }, [code, onTextChange, serverUrl]);

  return (
    <div className="flex-1 overflow-y-auto h-full" style={{height:"calc(100dvh - 50px)"}} ref={containerRef}></div>
  );
}
