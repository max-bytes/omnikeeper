import { Breadcrumb } from "antd";
import React from "react";
import { Link, withRouter } from "react-router-dom";
import _ from 'lodash';

const breadcrumbNameMap = [
  { search: '^/manage/base-configuration', name: 'Base Configuration' },
  { search: '^/manage/layers/operations/(.*)', name: 'Statistics & Operations For Layer %1%' },
  { search: '^/manage/layers/operations', skip: true },
  { search: '^/manage/layers', name: 'Layers' },
  { search: '^/manage/oiacontexts', name: 'Online Inbound Adapter Contexts' },
  { search: '^/manage/odataapicontexts', name: 'OData API Contexts' },
  { search: '^/manage/predicates', name: 'Predicates' },
  { search: '^/manage/traits', name: 'Traits' },
  { search: '^/manage/auth-roles', name: 'Auth Roles' },
  { search: '^/manage/cache', name: 'Debug Cache' },
  { search: '^/manage/version', name: 'Version' },
  { search: '^/manage/current-user', name: 'Debug Current User' },
  { search: '^/manage/logs', name: 'Debug-Logs' },
  { search: '^/manage$', name: 'Manage' },
  
  { search: '^/explorer/(.*)', name: 'CI %1%' },
  { search: '^/explorer', name: 'Explore CIs', skipIfLast: true },
  
  { search: '^/changesets/(.*)', name: 'Changeset %1%' },
  { search: '^/changesets', name: 'Changesets', skipIfLast: true },

  { search: '^/diffing', name: 'Diffing', skipIfLast: true },
  
  { search: '^/grid-view/explorer/(.*)', skip: true },
  { search: '^/grid-view/explorer', skip: true },
  { search: '^/grid-view/create-context', skip: true },
  { search: '^/grid-view$', skip: true },
  
  { search: '/createCI', name: 'Create', skipIfLast: true },
  
  { search: '/traits/(.*)', name: 'Trait %1%' },
  { search: '/traits', name: 'Traits', skipIfLast: true },

  // TODO: plugins?
];

function pathname2BreadcrumbItems(pathname) {
  const pathSnippets = pathname.split('/').filter(i => i);

  var breadcrumbNameMapWorking = breadcrumbNameMap.slice();

  const extraBreadcrumbItems = pathSnippets.map((__, index) => {
    const url = `/${pathSnippets.slice(0, index + 1).join('/')}`;

    var mapItemIndex = _.findIndex(breadcrumbNameMapWorking, function(o) { 
      const m = url.match(o.search);
      if (m)
        return true;
      return false;
    });

    if (mapItemIndex !== -1) {
      const mapItem = _.pullAt(breadcrumbNameMapWorking, [mapItemIndex])[0];

      if (mapItem.skip) {
        return undefined;
      }
      if (mapItem.skipIfLast && index === pathSnippets.length - 1) {
        return undefined;
      }

      const m = url.match(mapItem.search);
      const name = mapItem.name.replace(/%(\d+)%/g, function(a, index) {
        return m[index];
      });


      return <Breadcrumb.Item key={url}>
        <Link to={url}>{name}</Link>
      </Breadcrumb.Item>;
    } else {
      return null;//<Breadcrumb.Item key={url}>[Unknown]</Breadcrumb.Item>;
    }
  }).filter(i => i);

  // skip last if empty
  // if (extraBreadcrumbItems.length > 0 && _.last(extraBreadcrumbItems).skipIfLast)
  //   extraBreadcrumbItems.pop();

  return extraBreadcrumbItems;
}


function Breadcrumbs(props) {
  const { location, style } = props;
  
  const extraBreadcrumbItems = pathname2BreadcrumbItems(location.pathname);

  if (extraBreadcrumbItems.length === 0)
    return null;

  const breadcrumbItems = [
    <Breadcrumb.Item key="home">
        <Link to="/">Home</Link>
    </Breadcrumb.Item>,
    ].concat(extraBreadcrumbItems);

  return <Breadcrumb style={style}>{breadcrumbItems}</Breadcrumb>;
}

export default withRouter(Breadcrumbs);