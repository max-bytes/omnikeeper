import _ from 'lodash';

export function calculateNodeWidth(node) {
    // Create element with attributes needed to calculate text size
    const ctx = document.createElement('canvas').getContext("2d");
    const fStyle = node.pstyle('font-style').strValue;
    const size = node.pstyle('font-size').pfValue + 'px';
    const family = node.pstyle('font-family').strValue;
    const weight = node.pstyle('font-weight').strValue;
    ctx.font = fStyle + ' ' + weight + ' ' + size + ' ' + family;

    // For multiple lines, evaluate the width of the largest line
    const lines = node.data('label').split('\n')
    const lengths = lines.map(a => a.length);
    const max_line = lengths.indexOf(Math.max(...lengths));

    // User-defined padding
    const padding = 20

    return Math.max(350, ctx.measureText(lines[max_line]).width + padding);
}

export function calculateNodeHeight(node) {
    // Create element with attributes needed to calculate text size
    const ctx = document.createElement('canvas').getContext("2d");
    const fStyle = node.pstyle('font-style').strValue;
    const size = node.pstyle('font-size').pfValue + 'px';
    const family = node.pstyle('font-family').strValue;
    const weight = node.pstyle('font-weight').strValue;
    ctx.font = fStyle + ' ' + weight + ' ' + size + ' ' + family;
    const lines = node.data('label').split('\n');

    // User-defined padding
    const padding = 20

    return _.sum(_.map(lines, line => {
        const tm = ctx.measureText(line);
        return tm.actualBoundingBoxAscent + tm.actualBoundingBoxDescent;
    })) + padding;
}
