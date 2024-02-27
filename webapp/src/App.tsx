import { useState } from 'react'
import './App.css'
import { Button } from "@/components/ui/button";
import { CodeEditor } from "@/components/code-editor";
import { ResultsContainer } from "@/components/results-container";

// TODO this should be configure using env vars
const EDITOR_SERVICE_URL = "ws://localhost:5176/mirrorsharp";
const API_URL = "http://localhost:5176";

const INITIAL_CODE = `// visit https://benchmarkdotnet.org/ for more info on BenchmarkDotNet

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

// Don't change this namespace.
namespace SharpbenchBenchmark;

[MemoryDiagnoser]
public class Benchmarks
{
    // Write your benchmarks here
    const int size = 1_000_000_000;
    private int[] array = new int[size];

    public Benchmarks()
    {
      for (int i = 0; i < size; i++)
      {
        array[i] = i + 1;
      }
    }

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
        <div className="flex-1 h-full">
          <CodeEditor
            serverUrl={EDITOR_SERVICE_URL}
            initialCode={INITIAL_CODE}
            onTextChange={setCode}
          />
        </div>
        <div className="flex-1 h-full border-l border-l-gray-200">
          <ResultsContainer />
        </div>
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

