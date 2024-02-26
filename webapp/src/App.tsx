import { useState } from 'react'
import './App.css'
import { Button } from "@/components/ui/button";
import { CodeEditor } from "@/components/code-editor";

// TODO this should be configure using env vars
const EDITOR_SERVICE_URL = "ws://localhost:5176/mirrorsharp";
const API_URL = "http://localhost:5176";

const INITIAL_CODE = `
public class MyBenchmark
{
    private int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Benchmark]
    public int ArraySum()
    {
        int sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i];
        }

        return sum;
    }
}
`

export function App() {
  const [code, setCode] = useState(INITIAL_CODE);

  function handleRun() {
    if (!code) return;
    submitCodeRun(code);
  }

  return (
    <main className="h-screen bg-red flex flex-col">
      <div className="h-[50px] px-2 flex items-center border-b border-b-gray-200 shadow-sm">
        <div className="mr-5">
          <span className="font-semibold">sharpbench</span>
        </div>
        <Button onClick={handleRun}>Run</Button>
      </div>
      <div className="flex flex-1" style={{height:"calc(100dvh - 50px)"}}>
        <CodeEditor
          serverUrl={EDITOR_SERVICE_URL}
          initialCode={INITIAL_CODE}
          onTextChange={setCode}
        />
      </div>
    </main>
  )
}

async function submitCodeRun(code: string) {
  console.log('submitting code run', code);
  const resp = await fetch(`${API_URL}/run`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      code: code
    }),
    mode: 'cors'
  });

  const data = await resp.json();
  console.log('code run result', data);
  return data;
}

