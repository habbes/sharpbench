import { Job } from '@/types';
import Markdown from "react-markdown";
import remarkGfm from 'remark-gfm'

export function ReportContainer({ job } : { job : Job }) {
  return (
    <div className="p-4">
    {
      job.status === 'Completed' ?
        <MarkdownReport content={job.markdownReport} />
      : job.status === 'Error' ?
        <div className="font-mono">
          Execution failed with exit code { job.exitCode}. Check out the logs for more details.
        </div>
      :
        <div>Results not ready. Check again after the job is completed.</div>
    }
    </div>
  );
}


function MarkdownReport({ content } : { content?: string }) {
  return (
    <Markdown
      remarkPlugins={[remarkGfm]}
      components={{
        tr(props) {
          // eslint-disable-next-line @typescript-eslint/no-unused-vars
          const { node, ...rest } = props;
          return <tr className="border" {...rest} />
        },
        th(props) {
          // eslint-disable-next-line @typescript-eslint/no-unused-vars
          const { node, ...rest} = props;
          return <th className="border py-2 px-4" {...rest} />
        },
        td(props) {
          // eslint-disable-next-line @typescript-eslint/no-unused-vars
          const { node, ...rest } = props;
          return <td className="border py-2 px-4" {...rest} />
        }
      }}
    >
      {content}
    </Markdown>
  )
}