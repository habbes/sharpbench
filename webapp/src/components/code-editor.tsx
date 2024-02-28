import { useRef, useEffect } from 'react'
import "mirrorsharp-codemirror-6-preview/mirrorsharp.css";
import mirrorsharp, { MirrorSharpInstance } from 'mirrorsharp-codemirror-6-preview';

export interface CodeEditorProps {
    serverUrl: string;
    onTextChange?: (text: string) => void;
    initialCode?: string;
}

const LANGUAGE = 'C#';

export function CodeEditor({
    onTextChange,
    initialCode,
    serverUrl
} : CodeEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const ms = useRef<MirrorSharpInstance<void>>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    if (ms.current) return;
    console.log('rendering ms');
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
    ms.current = mirrorsharp(
      containerRef.current, {
        serviceUrl: serverUrl,
        language: LANGUAGE,
        text: initialCode,
        serverOptions: { 'x-mode': 'regular' },
        on: {
            textChange: getText => onTextChange && onTextChange(getText())
        }
      });
    
  },);

  return (
    <div className="flex-1 overflow-y-auto h-full" style={{height:"calc(100dvh - 50px)"}} ref={containerRef}></div>
  );
}
