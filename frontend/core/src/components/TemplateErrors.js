// import React from 'react';
// import { Button } from 'antd';

// function TemplateErrors(props) {

//   return (<>
//     {props.templateErrors.attributeErrors.length > 0 && 
//     <div>Errors:
//       {props.templateErrors.attributeErrors.map(ae => 
//       <div key={ae.attributeName}>{ae.attributeName}:
//         <ul>
//           {ae.errors.map(e => {

//             let actions = <></>;
//             switch (e.__typename) {
//               case "TemplateErrorAttributeMissingType":
//                 actions = <Button size='small' onClick={t => props.onCreateNewAttribute(ae.attributeName, e.type)}>Create</Button>
//                 break;
//               case "TemplateErrorAttributeWrongTypeType":
//                 actions = <Button size='small' onClick={t => props.onOverwriteAttribute(ae.attributeName, e.correctTypes[0])}>Update</Button>
//                 break;
//                 case "TemplateErrorAttributeGenericType":
//                   break;
//               default:
//                 break;
//             }

//             return <li key={e.__typename}>
//               {e.errorMessage}
//               {actions}
//             </li>;
//           })}
//         </ul>
//       </div>)}
//     </div>}
//   </>);
// }

// export default TemplateErrors;